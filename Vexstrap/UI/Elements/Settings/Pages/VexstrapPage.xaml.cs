using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vexstrap.UI.ViewModels.Settings;

namespace Vexstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for VexstrapPage.xaml
    /// </summary>
    public partial class VexstrapPage
    {
        public VexstrapPage()
        {
            DataContext = new VexstrapViewModel();
            InitializeComponent();
        }
    }
}

