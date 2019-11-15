﻿using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Abyss
{
    public class CommandContextEnricher : ILogEventEnricher
    {
        private readonly AbyssCommandContext _context;

        public CommandContextEnricher(AbyssCommandContext context)
        {
            _context = context;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Context", new
            {
                Command = _context.Command?.FullAliases[0] ?? "None",
                Guild = _context.Guild.Id.RawValue,
                Invoker = _context.Invoker.Id.RawValue,
                Channel = _context.Channel.Id.RawValue,
                Parameters = _context.RawArguments
            }, true));
        }
    }
}
