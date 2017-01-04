using System;
using System.Linq;
using JetEntityFrameworkProvider.Test.Model03;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test
{
    [TestClass]
    public class JoinErrorTest1
    {
        [TestMethod]
        public void Run()
        {
            using (Context context = new Context(Helpers.GetConnection()))
            {
                int panelId = 12;
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                context.PanelTextures.AsNoTracking().Where(pt => pt.PanelId == panelId).Select(pt => pt.Texture);
            }
        }
    }
}
