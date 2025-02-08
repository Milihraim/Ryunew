﻿using FluentAvalonia.Core;
using MsgPack;
using Ryujinx.Ava.Utilities.AppLibrary;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Ava.Utilities.PlayReport
{
    /// <summary>
    /// A mapping of title IDs to value formatter specs.
    ///
    /// <remarks>Generally speaking, use the <see cref="Analyzer"/>.AddSpec(...) methods instead of creating this class yourself.</remarks>
    /// </summary>
    public class GameSpec
    {
        private int _lastPriority;
        
        public required string[] TitleIds { get; init; }

        public List<FormatterSpecBase> ValueFormatters { get; } = [];


        /// <summary>
        /// Add a value formatter to the current <see cref="GameSpec"/>
        /// matching a specific key that could exist in a Play Report for the previously specified title IDs.
        /// </summary>
        /// <param name="reportKey">The key name to match.</param>
        /// <param name="valueFormatter">The function which can return a potential formatted value.</param>
        /// <returns>The current <see cref="GameSpec"/>, for chaining convenience.</returns>
        public GameSpec AddValueFormatter(string reportKey, ValueFormatter valueFormatter)
            => AddValueFormatter(_lastPriority++, reportKey, valueFormatter);

        /// <summary>
        /// Add a value formatter at a specific priority to the current <see cref="GameSpec"/>
        /// matching a specific key that could exist in a Play Report for the previously specified title IDs.
        /// </summary>
        /// <param name="priority">The resolution priority of this value formatter. Higher resolves sooner.</param>
        /// <param name="reportKey">The key name to match.</param>
        /// <param name="valueFormatter">The function which can return a potential formatted value.</param>
        /// <returns>The current <see cref="GameSpec"/>, for chaining convenience.</returns>
        public GameSpec AddValueFormatter(int priority, string reportKey,
            ValueFormatter valueFormatter)
        {
            ValueFormatters.Add(new FormatterSpec
            {
                Priority = priority, ReportKeys = [reportKey], Formatter = valueFormatter
            });
            return this;
        }

        /// <summary>
        /// Add a multi-value formatter to the current <see cref="GameSpec"/>
        /// matching a specific set of keys that could exist in a Play Report for the previously specified title IDs.
        /// </summary>
        /// <param name="reportKeys">The key names to match.</param>
        /// <param name="valueFormatter">The function which can format the values.</param>
        /// <returns>The current <see cref="GameSpec"/>, for chaining convenience.</returns>
        public GameSpec AddMultiValueFormatter(string[] reportKeys, MultiValueFormatter valueFormatter)
            => AddMultiValueFormatter(_lastPriority++, reportKeys, valueFormatter);

        /// <summary>
        /// Add a multi-value formatter at a specific priority to the current <see cref="GameSpec"/>
        /// matching a specific set of keys that could exist in a Play Report for the previously specified title IDs.
        /// </summary>
        /// <param name="priority">The resolution priority of this value formatter. Higher resolves sooner.</param>
        /// <param name="reportKeys">The key names to match.</param>
        /// <param name="valueFormatter">The function which can format the values.</param>
        /// <returns>The current <see cref="GameSpec"/>, for chaining convenience.</returns>
        public GameSpec AddMultiValueFormatter(int priority, string[] reportKeys,
            MultiValueFormatter valueFormatter)
        {
            ValueFormatters.Add(new MultiFormatterSpec
            {
                Priority = priority, ReportKeys = reportKeys, Formatter = valueFormatter
            });
            return this;
        }

        /// <summary>
        /// Add a multi-value formatter to the current <see cref="GameSpec"/>
        /// matching a specific set of keys that could exist in a Play Report for the previously specified title IDs.
        /// <br/><br/>
        /// The 'Sparse' multi-value formatters do not require every key to be present.
        /// If you need this requirement, use <see cref="AddMultiValueFormatter(string[], Ryujinx.Ava.Utilities.PlayReport.MultiValueFormatter)"/>.
        /// </summary>
        /// <param name="reportKeys">The key names to match.</param>
        /// <param name="valueFormatter">The function which can format the values.</param>
        /// <returns>The current <see cref="GameSpec"/>, for chaining convenience.</returns>
        public GameSpec AddSparseMultiValueFormatter(string[] reportKeys, SparseMultiValueFormatter valueFormatter)
            => AddSparseMultiValueFormatter(_lastPriority++, reportKeys, valueFormatter);

        /// <summary>
        /// Add a multi-value formatter at a specific priority to the current <see cref="GameSpec"/>
        /// matching a specific set of keys that could exist in a Play Report for the previously specified title IDs.
        /// <br/><br/>
        /// The 'Sparse' multi-value formatters do not require every key to be present.
        /// If you need this requirement, use <see cref="AddMultiValueFormatter(int, string[], Ryujinx.Ava.Utilities.PlayReport.MultiValueFormatter)"/>.
        /// </summary>
        /// <param name="priority">The resolution priority of this value formatter. Higher resolves sooner.</param>
        /// <param name="reportKeys">The key names to match.</param>
        /// <param name="valueFormatter">The function which can format the values.</param>
        /// <returns>The current <see cref="GameSpec"/>, for chaining convenience.</returns>
        public GameSpec AddSparseMultiValueFormatter(int priority, string[] reportKeys,
            SparseMultiValueFormatter valueFormatter)
        {
            ValueFormatters.Add(new SparseMultiFormatterSpec
            {
                Priority = priority, ReportKeys = reportKeys, Formatter = valueFormatter
            });
            return this;
        }
    }

    /// <summary>
    /// A struct containing the data for a mapping of a key in a Play Report to a formatter for its potential value.
    /// </summary>
    public class FormatterSpec : FormatterSpecBase
    {
        public override bool GetData(Horizon.Prepo.Types.PlayReport playReport, out object result)
        {
            if (!playReport.ReportData.AsDictionary().TryGetValue(ReportKeys[0], out MessagePackObject valuePackObject))
            {
                result = null;
                return false;
            }

            result = valuePackObject;
            return true;
        }
    }

    /// <summary>
    /// A struct containing the data for a mapping of an arbitrary key set in a Play Report to a formatter for their potential values.
    /// </summary>
    public class MultiFormatterSpec : FormatterSpecBase
    {
        public override bool GetData(Horizon.Prepo.Types.PlayReport playReport, out object result)
        {
            List<MessagePackObject> packedObjects = [];
            foreach (var reportKey in ReportKeys)
            {
                if (!playReport.ReportData.AsDictionary().TryGetValue(reportKey, out MessagePackObject valuePackObject))
                {
                    result = null;
                    return false;
                }

                packedObjects.Add(valuePackObject);
            }

            result = packedObjects;
            return true;
        }
    }

    /// <summary>
    /// A struct containing the data for a mapping of an arbitrary key set in a Play Report to a formatter for their sparsely populated potential values.
    /// </summary>
    public class SparseMultiFormatterSpec : FormatterSpecBase
    {
        public override bool GetData(Horizon.Prepo.Types.PlayReport playReport, out object result)
        {
            Dictionary<string, MessagePackObject> packedObjects = [];
            foreach (var reportKey in ReportKeys)
            {
                if (!playReport.ReportData.AsDictionary().TryGetValue(reportKey, out MessagePackObject valuePackObject))
                    continue;

                packedObjects.Add(reportKey, valuePackObject);
            }

            result = packedObjects;
            return true;
        }
    }
    
    public abstract class FormatterSpecBase
    {
        public abstract bool GetData(Horizon.Prepo.Types.PlayReport playReport, out object data);
        
        public int Priority { get; init; }
        public string[] ReportKeys { get; init; }
        public Delegate Formatter { get; init; }

        public bool Format(ApplicationMetadata appMeta, Horizon.Prepo.Types.PlayReport playReport, out FormattedValue formattedValue)
        {
            formattedValue = default;
            if (!GetData(playReport, out object data))
                return false;

            if (data is FormattedValue fv)
            {
                formattedValue = fv;
                return true;
            }

            if (Formatter is ValueFormatter vf && data is MessagePackObject mpo)
            {
                formattedValue = vf(new SingleValue(mpo) { Application = appMeta, PlayReport = playReport });
                return true;
            }

            if (Formatter is MultiValueFormatter mvf && data is List<MessagePackObject> messagePackObjects)
            {
                formattedValue = mvf(new MultiValue(messagePackObjects) { Application = appMeta, PlayReport = playReport });
                return true;
            }

            if (Formatter is SparseMultiValueFormatter smvf &&
                data is Dictionary<string, MessagePackObject> sparseMessagePackObjects)
            {
                formattedValue = smvf(new SparseMultiValue(sparseMessagePackObjects) { Application = appMeta, PlayReport = playReport });
                return true;
            }

            throw new InvalidOperationException("Formatter delegate is not of a known type!");
        }
    }
}
