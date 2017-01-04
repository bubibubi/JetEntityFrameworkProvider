using System;

namespace JetEntityFrameworkProvider.Test.Model43_PKasFK
{
    static class ParentsHelper
    {
        public static void AddOrUpdate(this Context context, string name, Parent parent)
        {
            var dbParent = context.Parents.Find(name);
            if (dbParent == null)
                context.Parents.Add(parent);
            else
                throw new NotImplementedException();

            context.SaveChanges();

        }
    }
}
