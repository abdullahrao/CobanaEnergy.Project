using Autofac;
using Autofac.Integration.Mvc;
using CobanaEnergy.Project.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Filters
{
    public static class AutofacConfig
    {
        public static void RegisterDependencies()
        {
            var builder = new ContainerBuilder();
            builder.RegisterControllers(Assembly.GetExecutingAssembly());

            builder.RegisterType<ApplicationDBContext>()
                   .AsSelf()
                   .InstancePerRequest();

            var container = builder.Build();

            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
        }
    }
}

//Autofac Lifetimes
//builder.RegisterType<MyService>().As<IMyService>().SingleInstance();- Singleton
//.InstancePerRequest(); - Scoped / Per HTTP Request
//builder.RegisterType<MyUtility>().As<IMyUtility>().InstancePerDependency();- Transient / New Instance
//builder.RegisterType<MyService>().As<IMyService>().InstancePerLifetimeScope();- Per Lifetime Scope (Manual scopes)