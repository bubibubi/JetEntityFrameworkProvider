using System;

namespace JetEntityFrameworkProvider.Test.Model36_DetachedScenario
{
    static class Repository
    {
        public static void Update(Context context, Holder holder)
        {
            var thing = holder.Thing;
            holder.Thing = new Thing() {Id = 2};
            var attachedHolder = context.Holders.Attach(holder);
            attachedHolder.Thing = thing;
            context.Entry(holder).Property("Some").IsModified = true;

            //var manager = ((IObjectContextAdapter)context).ObjectContext.ObjectStateManager;
            //manager.ChangeRelationshipState(holder, holder.Thing, "Thing", EntityState.Added);
            
            context.SaveChanges();
        }



    }

}