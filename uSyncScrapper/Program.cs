using System;
using System.Windows.Forms;
using Autofac;
using uSyncScrapper.Context;
using uSyncScrapper.Repositories;

namespace uSyncScrapper
{
    internal static class Program
    {
        private static IContainer container;

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Bootstrap();
            Application.Run(container.Resolve<Form1>());
        }

        private static void Bootstrap()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<LocalContext>().As<ILocalContext>().SingleInstance();
            builder.RegisterType<ContentTypeRepository>().As<IContentTypeRepository>().SingleInstance();
            builder.RegisterType<DataTypeRepository>().As<IDataTypeRepository>().SingleInstance();
            builder.RegisterType<BlueprintRepository>().As<IBlueprintRepository>().SingleInstance();
            builder.RegisterType<Form1>();

            container = builder.Build();
        }
    }
}