﻿using PizzaApi.MessageContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit;
using Topshelf;
using NLog;
using MassTransit.NLogIntegration;
using MassTransit.Logging;
using GreenPipes;

namespace PizzaDesktopApp.Attendant
{
    public class AttendantService : ServiceControl
    {
        private BusHandle _busHandle;
        private IBusControl _bus;

        public bool Start(HostControl hostControl)
        {
            _bus = BusConfigurator.ConfigureBus((cfg, host) =>
            {
                cfg.ReceiveEndpoint(host, RabbitMqConstants.RegisterOrderServiceQueue, e =>
                {
                    cfg.UseNLog(new LogFactory());

                    e.UseRateLimit(1, TimeSpan.FromSeconds(30));

                    e.UseRetry(Retry.Interval(5, TimeSpan.FromSeconds(5)));

                    e.UseCircuitBreaker(cb =>
                    {
                        cb.TripThreshold = 15;
                        //cb.ResetInterval(TimeSpan.FromMinutes(5));
                        cb.ResetInterval = TimeSpan.FromSeconds(5);
                        cb.TrackingPeriod = TimeSpan.FromMinutes(1);
                        cb.ActiveThreshold = 10;
                    });

                    e.Consumer<OrderRegisteredConsumer>();

                });
            });

            var consumeObserver = new LogConsumeObserver();
            _bus.ConnectConsumeObserver(consumeObserver);

            //_busHandle = bus.Start();
            _bus.Start();
            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            //if (_busHandle != null)
            //    _busHandle.Stop();
            
            _bus.Stop();

            return true;
        }
    }
}
