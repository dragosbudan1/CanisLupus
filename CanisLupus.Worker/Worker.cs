using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CanisLupus.Worker.Account;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using CanisLupus.Worker.Events;

namespace CanisLupus.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger logger;
        private readonly IMarketMakerHandler marketMakerHandler;
        private readonly ITradingSettingsService tradingSettingsService;
        private readonly IInsertTradingSettingsRpcServer insertTradingSettingsRpcServer;
        private readonly IGetTradingSettingsRpcServer getTradingSettingsRpcServer;
        private readonly IDeleteTradingSettingsRpcServer deleteTradingSettingsRpcServer;
        private readonly IUpdateTradingSettingsRpcServer updateTradingSettingsRpcServer;

        public Worker(IMarketMakerHandler marketMakerHandler,
            ITradingSettingsService tradingSettingsService,
            IInsertTradingSettingsRpcServer insertTradingSettingsRpcServer,
            IGetTradingSettingsRpcServer getTradingSettingsRpcServer,
            IDeleteTradingSettingsRpcServer deleteTradingSettingsRpcServer,
            IUpdateTradingSettingsRpcServer updateTradingSettingsRpcServer)
        {
            this.insertTradingSettingsRpcServer = insertTradingSettingsRpcServer;
            this.getTradingSettingsRpcServer = getTradingSettingsRpcServer;
            this.deleteTradingSettingsRpcServer = deleteTradingSettingsRpcServer;
            this.updateTradingSettingsRpcServer = updateTradingSettingsRpcServer;
            this.tradingSettingsService = tradingSettingsService;
            this.logger = LogManager.GetCurrentClassLogger();
            this.marketMakerHandler = marketMakerHandler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            StartListeningToInsertTradingSettingsRpcRequests(stoppingToken);
            StartListeningToGetTradingSettingsRpcRequests(stoppingToken);
            StartListeningToDeleteTradingSettingsRpcRequests(stoppingToken);
            StartListeningToUpdateTradingSettingsRpcRequests(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    logger.Info("Worker running at: {time}", DateTimeOffset.Now);

                    var tradingSettingsList = await tradingSettingsService.GetAllAsync();

                    foreach (var settings in tradingSettingsList)
                    {
                        await marketMakerHandler.ExecuteAsync(settings);
                    }

                }
                catch (System.Exception ex)
                {

                    logger.Error(ex, ex.Message);
                }

                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }

        private void StartListeningToInsertTradingSettingsRpcRequests(CancellationToken stoppingToken)
        {
            Task task = new Task(async () =>
            {
                await insertTradingSettingsRpcServer.InitialiseAsync();
                while (!stoppingToken.IsCancellationRequested)
                {
                    await insertTradingSettingsRpcServer.ListenAsync();
                }
                insertTradingSettingsRpcServer.Close();
            });
            task.Start();
        }

        private void StartListeningToGetTradingSettingsRpcRequests(CancellationToken stoppingToken)
        {
            Task task = new Task(async () =>
            {
                await getTradingSettingsRpcServer.InitialiseAsync();
                while (!stoppingToken.IsCancellationRequested)
                {
                    await getTradingSettingsRpcServer.ListenAsync();
                }
                getTradingSettingsRpcServer.Close();
            });
            task.Start();
        }

        private void StartListeningToDeleteTradingSettingsRpcRequests(CancellationToken stoppingToken)
        {
            Task task = new Task(async () =>
            {
                await deleteTradingSettingsRpcServer.InitialiseAsync();
                while (!stoppingToken.IsCancellationRequested)
                {
                    await deleteTradingSettingsRpcServer.ListenAsync();
                }
                deleteTradingSettingsRpcServer.Close();
            });
            task.Start();
        }

        private void StartListeningToUpdateTradingSettingsRpcRequests(CancellationToken stoppingToken)
        {
            Task task = new Task(async () =>
            {
                await updateTradingSettingsRpcServer.InitialiseAsync();
                while (!stoppingToken.IsCancellationRequested)
                {
                    await updateTradingSettingsRpcServer.ListenAsync();
                }
                updateTradingSettingsRpcServer.Close();
            });
            task.Start();
        }
    }
}
