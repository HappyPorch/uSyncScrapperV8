﻿using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace uSyncScrapper
{
    static class Program
    {
        private static IContainer container;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Bootstrap();
            Application.Run(container.Resolve<Form1>());
        }

        private static void Bootstrap()
        {
            var builder = new ContainerBuilder();

            //builder.RegisterType<LocalContext>().As<ILocalContext>().SingleInstance();
            //builder.RegisterType<ContentTypeMapper>();
            //builder.RegisterGeneric(typeof(Repository<>))
            //    .As(typeof(IRepository<>))
            //    .SingleInstance();
            builder.RegisterType<Form1>();

            container = builder.Build();
        }
    }
}