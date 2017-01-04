using System;
using System.Data.Entity.Infrastructure.Interception;

namespace JetEntityFrameworkProvider.Test.Model21_CommandInterception
{
    class DbCommandTreeInterceptor : IDbCommandTreeInterceptor
    {
        public void TreeCreated(DbCommandTreeInterceptionContext interceptionContext)
        {
            Console.WriteLine(interceptionContext.OriginalResult);
        }
    }
}
