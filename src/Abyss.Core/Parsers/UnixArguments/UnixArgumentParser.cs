﻿using System;
using System.Collections.Generic;
using System.Text;
using Qmmands;

namespace Abyss.Core.Parsers.UnixArguments
{
    internal enum UnixParserState
    {
        Neutral,
        ArgumentName,
        ArgumentValue,
        DashSequence
    }

    public class UnixArgumentParser : IArgumentParser
    {
        public static readonly UnixArgumentParser Instance = new UnixArgumentParser();

        private UnixArgumentParser() { }

        private static readonly Type _booleanType = typeof(bool);

        private Parameter? GetParameter(CommandContext context, string name)
        {
            for (var i = 0; i < context.Command.Parameters.Count; i++)
            {
                var parameter = context.Command.Parameters[i];
                if (parameter.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return parameter;
            }
            return null;
        }

        ArgumentParserResult IArgumentParser.Parse(CommandContext context) => Parse(context);

        public UnixArgumentParserResult Parse(CommandContext context)
        {
            var state = UnixParserState.Neutral;
            var parameters = new Dictionary<Parameter, object>();
            var inQuote = false;
            var parameterName = new StringBuilder();
            var parameterValue = new StringBuilder();
            var rawArguments = new StringBuilder(context.RawArguments).Append(' '); // big smart plays

            for (var tokenPosition = 0; tokenPosition < rawArguments.Length; tokenPosition++)
            {
                var token = rawArguments[tokenPosition];

                switch (token)
                {
                    // second dash in dash sequence
                    case '-' when state == UnixParserState.DashSequence:
                        state = UnixParserState.ArgumentName;
                        break;
                    // first dash in dash sequence
                    case '-':
                        state = UnixParserState.DashSequence;
                        break;

                    // value set
                    case '=' when state == UnixParserState.ArgumentName:
                        state = UnixParserState.ArgumentValue;
                        break;

                    // handle spaces when in quote
                    case ' ' when inQuote:
                        parameterValue.Append(' ');
                        break;

                    // if the argument name is interrupted, check for a boolean (making sure not to overwrite a default value) otherwise return error
                    case ' ' when state == UnixParserState.ArgumentName:
                        {
                            var parameter = GetParameter(context, parameterName.ToString());
                            if (parameter != null && parameter.Type == _booleanType)
                            {
                                parameters.TryAdd(parameter, true);
                                parameterName.Clear();
                                parameterValue.Clear();
                                state = UnixParserState.Neutral;
                            }
                            else
                            {
                                state = UnixParserState.ArgumentValue;
                            }
                            break;
                        }

                    // end argument k/v
                    case ' ' when state == UnixParserState.ArgumentValue:
                        {
                            state = UnixParserState.Neutral;
                            var can = parameterName.ToString();
                            var parameter = GetParameter(context, can);
                            if (parameter == null) return UnixArgumentParserResult.UnknownParameter(context, parameters, can, tokenPosition);
                            parameters.TryAdd(parameter, parameterValue.ToString());
                            parameterName.Clear();
                            parameterValue.Clear();
                            break;
                        }

                    // quote start
                    case '"' when state == UnixParserState.ArgumentValue && !inQuote:
                        inQuote = true;
                        break;

                    // quote end
                    case '"' when state == UnixParserState.ArgumentValue && inQuote:
                        {
                            inQuote = false;
                            var can = parameterName.ToString();
                            var parameter = GetParameter(context, can);
                            if (parameter == null) return UnixArgumentParserResult.UnknownParameter(context, parameters, can, tokenPosition);
                            parameters.TryAdd(parameter, parameterValue.ToString());
                            parameterName.Clear();
                            parameterValue.Clear();
                            state = UnixParserState.Neutral;
                            break;
                        }

                    // unexpected quote
                    case '"':
                        return UnixArgumentParserResult.UnexpectedQuote(context, parameters, tokenPosition);

                    // data value
                    default:
                        if (state == UnixParserState.ArgumentName)
                        {
                            parameterName.Append(token);
                            break;
                        }
                        if (state == UnixParserState.ArgumentValue)
                        {
                            parameterValue.Append(token);
                            break;
                        }
                        break;
                }
            }

            // unclosed quote
            if (inQuote) return UnixArgumentParserResult.UnclosedQuote(context, parameters, rawArguments.Length);

            foreach (var expectedParameter in context.Command.Parameters)
            {
                if (!parameters.ContainsKey(expectedParameter))
                {
                    if (expectedParameter.Type == _booleanType && expectedParameter.DefaultValue == null)
                    {
                        parameters.TryAdd(expectedParameter, false);
                        continue;
                    }
                    if (expectedParameter.IsOptional)
                    {
                        parameters.TryAdd(expectedParameter, expectedParameter.DefaultValue!);
                        continue;
                    }
                    else
                    {
                        return UnixArgumentParserResult.TooFewArguments(context, parameters, expectedParameter);
                    }
                }
            }

            return UnixArgumentParserResult.Successful(context, parameters);
        }
    }
}