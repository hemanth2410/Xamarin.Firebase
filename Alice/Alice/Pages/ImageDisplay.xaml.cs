using Alice.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Alice.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ImageDisplay : ContentPage
    {
        public ImageDisplay()
        {
            InitializeComponent();
            ImageDisplayView.Source = ChatPage.TargetURL;
            ImageDisplayView.Aspect = Aspect.AspectFit;
        }
    }
}