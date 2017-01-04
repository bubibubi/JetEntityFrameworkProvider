using System;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model10
{
    public abstract class Test
    {
#warning This test does not work
        //[TestMethod]
        public void Run()
        {
            using (DbConnection connection = GetConnection())
            using (TestContext db = new TestContext(connection))
            {
                var someClass = new SomeClass() {Name = "A"};
                someClass.Behavior = new BehaviorA() {BehaviorASpecific = "Behavior A"};
                db.SomeClasses.Add(someClass);

                // Here I have two classes with the state of added which make sense
                var modifiedEntities = db.ChangeTracker.Entries()
                    .Where(entity => entity.State != System.Data.Entity.EntityState.Unchanged).ToList();
                // They save with no problem
                db.SaveChanges();

                //someClass.Behavior = null;
                //db.SaveChanges();

                // Now I want to change the behavior and it causes entity to try to remove the behavior and add it again
                someClass.Behavior = new BehaviorB() {BehaviorBSpecific = "Behavior B"};

                // Here it can be seen that we have a behavior A with the state of deleted and 
                // behavior B with the state of added
                modifiedEntities = db.ChangeTracker.Entries()
                    .Where(entity => entity.State != System.Data.Entity.EntityState.Unchanged).ToList();

                foreach (var modifiedEntity in modifiedEntities)
                    Console.WriteLine("{0} {1}", modifiedEntity.Entity, modifiedEntity.State);

                // But in reality when entity sends the query to the database it replaces the 
                // remove and insert with an update query (this can be seen in the SQL Profiler) 
                // which causes the discrimenator to remain the same where it should change.
                db.SaveChanges();

                Behavior b = db.Behaviors.First();
                someClass.Behavior = b;

                db.SaveChanges();

                someClass.Behavior = new BehaviorA() {BehaviorASpecific = "Behavior C"};

                db.SaveChanges();
            }
        }

        protected abstract DbConnection GetConnection();

    }
}
