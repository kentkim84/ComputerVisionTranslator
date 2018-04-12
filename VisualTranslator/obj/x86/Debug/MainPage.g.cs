﻿#pragma checksum "C:\Users\kentkim84\source\repos\VisualTranslator\VisualTranslator\MainPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "180D97EE39902AB70FD9A1A21DB3BFEC"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace VisualTranslator
{
    partial class MainPage : 
        global::Windows.UI.Xaml.Controls.Page, 
        global::Windows.UI.Xaml.Markup.IComponentConnector,
        global::Windows.UI.Xaml.Markup.IComponentConnector2
    {
        /// <summary>
        /// Connect()
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 14.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void Connect(int connectionId, object target)
        {
            switch(connectionId)
            {
            case 1:
                {
                    this.ViewPanel = (global::Windows.UI.Xaml.Controls.StackPanel)(target);
                }
                break;
            case 2:
                {
                    this.OCRControlPanel = (global::Windows.UI.Xaml.Controls.StackPanel)(target);
                }
                break;
            case 3:
                {
                    this.ProgressControlPanel = (global::Windows.UI.Xaml.Controls.StackPanel)(target);
                }
                break;
            case 4:
                {
                    this.ProgressBackground = (global::Windows.UI.Xaml.Controls.StackPanel)(target);
                }
                break;
            case 5:
                {
                    this.ProgresRing = (global::Windows.UI.Xaml.Controls.ProgressRing)(target);
                }
                break;
            case 6:
                {
                    this.ProgressMessage = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 7:
                {
                    this.StatusBorder = (global::Windows.UI.Xaml.Controls.Border)(target);
                }
                break;
            case 8:
                {
                    this.TranslatedTextBlock = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 9:
                {
                    this.OriginalTextBlock = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 10:
                {
                    this.StatusBlock = (global::Windows.UI.Xaml.Controls.TextBlock)(target);
                }
                break;
            case 11:
                {
                    this.OCRImageView = (global::Windows.UI.Xaml.Controls.Image)(target);
                }
                break;
            case 12:
                {
                    this.OCRTextOverlay = (global::Windows.UI.Xaml.Controls.Grid)(target);
                }
                break;
            case 13:
                {
                    this.PreviewMediaButton = (global::Windows.UI.Xaml.Controls.Button)(target);
                    #line 77 "..\..\..\MainPage.xaml"
                    ((global::Windows.UI.Xaml.Controls.Button)this.PreviewMediaButton).Tapped += this.PreviewMediaButton_Tapped;
                    #line default
                }
                break;
            case 14:
                {
                    this.OpenFileButton = (global::Windows.UI.Xaml.Controls.Button)(target);
                    #line 85 "..\..\..\MainPage.xaml"
                    ((global::Windows.UI.Xaml.Controls.Button)this.OpenFileButton).Tapped += this.OpenFileButton_Tapped;
                    #line default
                }
                break;
            case 15:
                {
                    this.QueryImageButton = (global::Windows.UI.Xaml.Controls.Button)(target);
                    #line 93 "..\..\..\MainPage.xaml"
                    ((global::Windows.UI.Xaml.Controls.Button)this.QueryImageButton).Tapped += this.QueryImageButton_Tapped;
                    #line default
                }
                break;
            case 16:
                {
                    this.CloudButton = (global::Windows.UI.Xaml.Controls.Button)(target);
                    #line 101 "..\..\..\MainPage.xaml"
                    ((global::Windows.UI.Xaml.Controls.Button)this.CloudButton).Tapped += this.CloudButton_Tapped;
                    #line default
                }
                break;
            case 17:
                {
                    this.ImagePreview = (global::Windows.UI.Xaml.Controls.CaptureElement)(target);
                }
                break;
            case 18:
                {
                    this.ImageView = (global::Windows.UI.Xaml.Controls.Image)(target);
                }
                break;
            default:
                break;
            }
            this._contentLoaded = true;
        }

        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 14.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::Windows.UI.Xaml.Markup.IComponentConnector GetBindingConnector(int connectionId, object target)
        {
            global::Windows.UI.Xaml.Markup.IComponentConnector returnValue = null;
            return returnValue;
        }
    }
}

