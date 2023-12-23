using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.MSAddinCLIPlugin.Builders
{
    internal class CSProjectSaver : BuilderBase
    {
        public override bool Build(BuilderContext context)
        {
            context.CSProjectDocument?.Save(context.CSProjectPath); return true;
        }
    }
}
