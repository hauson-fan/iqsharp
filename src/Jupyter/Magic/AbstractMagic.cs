﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.QsCompiler.Serialization;
using Newtonsoft.Json.Linq;

namespace Microsoft.Quantum.IQSharp.Jupyter
{
    /// <summary>
    ///     Abstract base class for IQ# magic symbols.
    /// </summary>
    public abstract class AbstractMagic : MagicSymbol
    {
        /// <summary>
        ///     Constructs a new magic symbol given its name and documentation.
        /// </summary>
        public AbstractMagic(string keyword, Documentation docs)
        {
            this.Name = $"%{keyword}";
            this.Documentation = docs;

            this.Kind = SymbolKind.Magic;
            this.Execute = SafeExecute(this.Run);
        }

        /// <summary>
        ///     Given a function representing the execution of a magic command,
        ///     returns a new function that executes <paramref name="magic" />
        ///     and catches any exceptions that occur during execution. The
        ///     returned execution function displays the given exceptions to its
        ///     display channel.
        /// </summary>
        public Func<string, IChannel, Task<ExecutionResult>> SafeExecute(Func<string, IChannel, ExecutionResult> magic) =>
            async (input, channel) =>
            {
                channel = channel.WithNewLines();

                try
                {
                    return magic(input, channel);
                }
                catch (InvalidWorkspaceException ws)
                {
                    foreach (var m in ws.Errors) channel.Stderr(m);
                    return ExecuteStatus.Error.ToExecutionResult();
                }
                catch (AggregateException agg)
                {
                    foreach (var e in agg.InnerExceptions) channel.Stderr(e?.Message);
                    return ExecuteStatus.Error.ToExecutionResult();
                }
                catch (Exception e)
                {
                    channel.Stderr(e.Message);
                    return ExecuteStatus.Error.ToExecutionResult();
                }
            };

        /// <summary>
        ///     Parses the input to a magic command, interpreting the input as
        ///     a name followed by a JSON-serialized dictionary.
        /// </summary>
        public static Dictionary<string, string> JsonToDict(string input) =>
            !string.IsNullOrEmpty(input) ? JsonConverters.JsonToDict(input) : new Dictionary<string, string> { };

        /// <summary>
        ///     Parses the input parameters for a given magic symbol and returns a
        ///     <c>Dictionary</c> with the names and values of the parameters,
        ///     where the values of the <c>Dictionary</c> are JSON-serialized objects.
        /// </summary>
        public static Dictionary<string, string> ParseInputParameters(string input, string firstParameterInferredName = "")
        {
            Dictionary<string, string> inputParameters = new Dictionary<string, string>();

            // This regex looks for four types of matches:
            // 1. (\{.*\})
            //      Matches anything enclosed in matching curly braces.
            // 2. [^\s"]+(?:\s*=\s*)(?:"[^"]*"|[^\s"]*)*
            //      Matches things that look like key=value, allowing whitespace around the equals sign,
            //      and allowing value to be a quoted string, e.g., key="value".
            // 3. [^\s"]+(?:"[^"]*"[^\s"]*)*
            //      Matches things that are single words, not inside quotes.
            // 4. (?:"[^"]*"[^\s"]*)+
            //      Matches quoted strings.
            var regex = new Regex(@"(\{.*\})|[^\s""]+(?:\s*=\s*)(?:""[^""]*""|[^\s""]*)*|[^\s""]+(?:""[^""]*""[^\s""]*)*|(?:""[^""]*""[^\s""]*)+");
            var args = regex.Matches(input).Select(match => match.Value);

            // If we are expecting a first inferred-name parameter, see if it exists.
            // If so, serialize it to the dictionary as JSON and remove it from the list of args.
            if (args.Any() &&
                !args.First().StartsWith("{") &&
                !args.First().Contains("=") &&
                !string.IsNullOrEmpty(firstParameterInferredName))
            {
                using var writer = new StringWriter();
                Json.Serializer.Serialize(writer, args.First());
                inputParameters[firstParameterInferredName] = writer.ToString();
                args = args.Skip(1);
            }

            // See if the remaining arguments look like JSON. If so, parse as JSON.
            if (args.Any() && args.First().StartsWith("{"))
            {
                var jsonArgs = JsonToDict(args.First());
                foreach (var (key, jsonValue) in jsonArgs)
                {
                    inputParameters[key] = jsonValue;
                }

                return inputParameters;
            }

            // Otherwise, try to parse as key=value pairs and serialize into the dictionary as JSON.
            foreach (string arg in args)
            {
                var tokens = arg.Split("=", 2);
                var key = tokens[0].Trim();
                var value = tokens.Length switch
                {
                    // If there was no value provided explicitly, treat it as an implicit "true" value
                    1 => true as object,

                    // Trim whitespace and also enclosing single-quotes or double-quotes before returning
                    2 => Regex.Replace(tokens[1].Trim(), @"^['""]|['""]$", string.Empty) as object,

                    // We called arg.Split("=", 2), so there should never be more than 2
                    _ => throw new InvalidOperationException()
                };
                using var writer = new StringWriter();
                Json.Serializer.Serialize(writer, value);
                inputParameters[key] = writer.ToString();
            }

            return inputParameters;
        }

        /// <summary>
        ///     A method to be run when the magic command is executed.
        /// </summary>
        public abstract ExecutionResult Run(string input, IChannel channel);
    }
}
