using System;
using System.Collections.Generic;
using System.Text;
////////////////////////
using System.Reflection;

[assembly: AssemblyTitle("Chat Client-Server Group 1")]
[assembly: AssemblyDescription("Chat form Private_public Client-Server Group 1")]
[assembly: AssemblyCompany(".........................")]
[assembly: AssemblyProduct("Chat trực tuyến ")]
[assembly: AssemblyCopyright("..........................")]
[assembly: CLSCompliant(true)]
[assembly: AssemblyVersion("1.0.0.0")]

namespace TCP_Private_Client
{
    class AssemblyInfo
    {
        private Type myType;

        public AssemblyInfo()
        {
            myType = typeof(Form_Main);
        }

        public string AsmName
        {

            get
            {

                return myType.Assembly.GetName().Name.ToString();

            }

        }

        public string AsmFQName
        {

            get
            {

                return myType.Assembly.GetName().FullName.ToString();

            }

        }

        public string CodeBase
        {

            get
            {

                return myType.Assembly.CodeBase;

            }

        }

        public string Copyright
        {

            get
            {

                Type at = typeof(AssemblyCopyrightAttribute);

                object[] r = myType.Assembly.GetCustomAttributes(at, false);

                AssemblyCopyrightAttribute ct = (AssemblyCopyrightAttribute)r[0];

                return ct.Copyright;

            }

        }

        public string Company
        {

            get
            {

                Type at = typeof(AssemblyCompanyAttribute);

                object[] r = myType.Assembly.GetCustomAttributes(at, false);

                AssemblyCompanyAttribute ct = (AssemblyCompanyAttribute)r[0];

                return ct.Company;

            }

        }

        public string Description
        {

            get
            {

                Type at = typeof(AssemblyDescriptionAttribute);

                object[] r = myType.Assembly.GetCustomAttributes(at, false);

                AssemblyDescriptionAttribute da = (AssemblyDescriptionAttribute)r[0];

                return da.Description;

            }

        }

        public string Product
        {

            get
            {

                Type at = typeof(AssemblyProductAttribute);

                object[] r = myType.Assembly.GetCustomAttributes(at, false);

                AssemblyProductAttribute pt = (AssemblyProductAttribute)r[0];

                return pt.Product;

            }

        }

        public string Title
        {

            get
            {

                Type at = typeof(AssemblyTitleAttribute);

                object[] r = myType.Assembly.GetCustomAttributes(at, false);

                AssemblyTitleAttribute ta = (AssemblyTitleAttribute)r[0];

                return ta.Title;

            }

        }

        public string Version
        {

            get
            {

                return myType.Assembly.GetName().Version.ToString();

            }

        }
    }
}
