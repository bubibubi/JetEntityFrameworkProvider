using System;
using System.Linq;

namespace JetEntityFrameworkProvider.Test.Model04
{
    public static class Seed
    {
        public static void SeedPersons(CarsContext context)
        {

            if (context.Persons.Count() != 0)
                return;

            for (int i = 0; i < 10; i++)
            {
                context.Persons.Add(new Person()
                {
                    Name = "PersonName " + (10 - i)
                }
                    );
            }

            context.SaveChanges();

        }
    }
}
