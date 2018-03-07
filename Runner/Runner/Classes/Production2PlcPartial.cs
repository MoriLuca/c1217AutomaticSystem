using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Runner.Classes
{
    public partial class production2plc
    {
        public bool IsEqualTo(production2plc test)
        {
            bool haveSameData;
            foreach (PropertyInfo prop in this.GetType().GetProperties())
            {
                haveSameData = prop.GetValue(this, null).Equals(prop.GetValue(test, null));

                if (!haveSameData)
                    return false;
            }
            return true;
        }
    }
}
