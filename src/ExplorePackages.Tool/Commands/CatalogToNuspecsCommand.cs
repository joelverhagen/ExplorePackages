﻿using System.Threading;
using System.Threading.Tasks;
using Knapcode.ExplorePackages.Logic;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using NuGet.CatalogReader;

namespace Knapcode.ExplorePackages.Tool.Commands
{
    public class CatalogToNuspecsCommand : ICommand
    {
        private readonly CatalogReader _catalogReader;
        private readonly CursorService _cursorService;
        private readonly CatalogToNuspecsProcessor _processor;
        private readonly ISingletonService _singletonService;
        private readonly ILogger<CatalogProcessorQueue> _logger;

        public CatalogToNuspecsCommand(
            CatalogReader catalogReader,
            CursorService cursorService,
            CatalogToNuspecsProcessor processor,
            ISingletonService singletonService,
            ILogger<CatalogProcessorQueue> logger)
        {
            _catalogReader = catalogReader;
            _cursorService = cursorService;
            _processor = processor;
            _singletonService = singletonService;
            _logger = logger;
        }

        public void Configure(CommandLineApplication app)
        {
        }

        public async Task ExecuteAsync(CancellationToken token)
        {
            var catalogProcessor = new CatalogProcessorQueue(
                _catalogReader,
                _cursorService,
                _processor,
                _singletonService,
                _logger);
            await catalogProcessor.ProcessAsync(token);
        }

        public bool IsInitializationRequired() => true;
        public bool IsDatabaseRequired() => true;
        public bool IsReadOnly() => false;
    }
}
