using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Effects;

namespace GateControl {
 public class AppearanceSettings { public int Theme=0; public bool Glass=true; public double Blur=20; public string Background=""; }
 public partial class Controller {
  AppearanceSettings appearance; ComboBox themePicker;CheckBox glassPicker;Slider blurPicker;TextBlock wallpaperLabel;bool changingAppearance;
  static readonly string[][] Themes={
   new[]{"设施绿 / 琥珀","#101716","#19231F","#15201A","#273728","#263530","#E9B65D","#344234","#3A4D44"},
   new[]{"深海蓝 / 冰青","#0D1423","#17263B","#101E32","#20364B","#253D55","#74D9ED","#294E64","#3C586F"},
   new[]{"暮光紫 / 薰衣草","#171222","#291F39","#211A30","#38294A","#3E3052","#D1B0FF","#503963","#635071"},
   new[]{"石墨灰 / 薄荷","#151719","#24282C","#1A1D21","#303B39","#343C40","#83E3B5","#354D45","#505B60"},
   new[]{"酒红 / 玫瑰金","#211318","#35232C","#291B23","#482E35","#50313C","#FFC0A5","#613D47","#73525E"}
  };
  void BuildAppearance(){
   try{appearance=Files.Load<AppearanceSettings>(Path.Combine(dataDir,"appearance.xml"));}catch{appearance=new AppearanceSettings();Status("外观配置无法读取，已使用默认样式。");}
   if(appearance.Theme<0||appearance.Theme>=Themes.Length)appearance.Theme=0;
   if(Double.IsNaN(appearance.Blur)||Double.IsInfinity(appearance.Blur))appearance.Blur=20;
   appearance.Blur=Math.Max(0,Math.Min(40,appearance.Blur));
   var nav=new RadioButton{Content="07    外观设置",Style=(Style)Window.FindResource("Nav")};Window.RegisterName("NavAppearance",nav);((StackPanel)C<RadioButton>("NavLogs").Parent).Children.Add(nav);
   var panel=new StackPanel{Margin=new Thickness(0,0,8,0)};var page=new ScrollViewer{Content=panel,Visibility=Visibility.Collapsed};Window.RegisterName("PageAppearance",page);((Grid)C<ScrollViewer>("PageLogs").Parent).Children.Add(page);nav.Checked+=(s,e)=>Navigate("Appearance");
   var card=new StackPanel();panel.Children.Add(new Border{Style=(Style)Window.FindResource("Card"),Child=card});
   card.Children.Add(new TextBlock{Text="让控制台拥有你的风格",Style=(Style)Window.FindResource("Section")});
   card.Children.Add(new TextBlock{Text="配色方案",Margin=new Thickness(0,0,0,8)});
   themePicker=new ComboBox();foreach(var t in Themes)themePicker.Items.Add(t[0]);themePicker.SelectedIndex=appearance.Theme;card.Children.Add(themePicker);
   var preview=new WrapPanel{Margin=new Thickness(0,12,0,18)};foreach(var t in Themes){var b=new Button{Content=t[0],Margin=new Thickness(0,0,8,8),Foreground=new SolidColorBrush((Color)ColorConverter.ConvertFromString(t[6])),Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString(t[2]))};int index=Array.IndexOf(Themes,t);b.Click+=(s,e)=>{themePicker.SelectedIndex=index;};preview.Children.Add(b);}card.Children.Add(preview);
   card.Children.Add(new TextBlock{Text="自定义背景",FontSize=18,Margin=new Thickness(0,12,0,12)});
   wallpaperLabel=new TextBlock{Style=(Style)Window.FindResource("Muted"),Margin=new Thickness(0,0,0,14)};card.Children.Add(wallpaperLabel);
   var actions=new WrapPanel();var pick=new Button{Content="选择 PNG / JPG 图片",Margin=new Thickness(0,0,10,0)};var clear=new Button{Content="移除背景"};actions.Children.Add(pick);actions.Children.Add(clear);card.Children.Add(actions);
   glassPicker=new CheckBox{Content="启用毛玻璃（背景模糊 + 半透明面板）",IsChecked=appearance.Glass,Margin=new Thickness(0,24,0,16)};card.Children.Add(glassPicker);
   card.Children.Add(new TextBlock{Text="背景模糊强度（0–40）"});blurPicker=new Slider{Minimum=0,Maximum=40,Value=appearance.Blur,TickFrequency=5,Margin=new Thickness(0,12,0,20),IsSnapToTickEnabled=true};System.Windows.Automation.AutomationProperties.SetName(blurPicker,"背景模糊强度");card.Children.Add(blurPicker);
   card.Children.Add(new TextBlock{Text="背景始终在内容后方；面板保留至少 90% 的深色底，文字和输入框不参与模糊。更换图片后会保存副本，原图片移动也不影响显示。外观自动保存，重启后恢复。",Style=(Style)Window.FindResource("Muted")});
   var reset=new Button{Content="恢复默认外观",HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,20,0,0)};card.Children.Add(reset);
   themePicker.SelectionChanged+=(s,e)=>SaveAppearance();glassPicker.Checked+=(s,e)=>SaveAppearance();glassPicker.Unchecked+=(s,e)=>SaveAppearance();blurPicker.ValueChanged+=(s,e)=>SaveAppearance();
   pick.Click+=(s,e)=>Guard(()=>{var dialog=new Microsoft.Win32.OpenFileDialog{Filter="背景图片|*.png;*.jpg;*.jpeg;*.bmp",Title="选择背景图片"};if(dialog.ShowDialog(Window)!=true)return;var bitmap=ReadWallpaper(dialog.FileName);string target=Path.Combine(dataDir,"wallpaper"+Path.GetExtension(dialog.FileName).ToLowerInvariant());if(!String.Equals(Path.GetFullPath(target),Path.GetFullPath(dialog.FileName),StringComparison.OrdinalIgnoreCase))Files.Atomic(target,File.ReadAllBytes(dialog.FileName),false);appearance.Background=target;SaveAppearance();});
   clear.Click+=(s,e)=>{appearance.Background="";SaveAppearance();};
   reset.Click+=(s,e)=>{changingAppearance=true;appearance=new AppearanceSettings();themePicker.SelectedIndex=0;glassPicker.IsChecked=true;blurPicker.Value=20;changingAppearance=false;SaveAppearance();};
   ApplyAppearance();
  }
  static BitmapImage ReadWallpaper(string path){
   if(!File.Exists(path))throw new FileNotFoundException("背景图片不存在。",path);
   if(new FileInfo(path).Length>32*1024*1024)throw new IOException("请选择小于 32 MB 的背景图片。");
   var bitmap=new BitmapImage();using(var stream=File.OpenRead(path)){bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.DecodePixelWidth=1920;bitmap.StreamSource=stream;bitmap.EndInit();}bitmap.Freeze();return bitmap;
  }
  void SaveAppearance(){if(changingAppearance)return;Guard(()=>{appearance.Theme=Math.Max(0,themePicker.SelectedIndex);appearance.Glass=glassPicker.IsChecked==true;appearance.Blur=blurPicker.Value;ApplyAppearance();Files.Save(Path.Combine(dataDir,"appearance.xml"),appearance);Status("外观已应用并保存。");});}
  void ApplyAppearance(){
   var t=Themes[appearance.Theme];string[] keys={"PageBg","CardBg","SideBg","HeroBg","ButtonBg","ThemeAccent","SelectedBg","LineBg"};
   for(int i=0;i<keys.Length;i++){var color=(Color)ColorConverter.ConvertFromString(t[i+1]);if(appearance.Glass&&(keys[i]=="CardBg"||keys[i]=="SideBg"||keys[i]=="HeroBg"))color.A=230;Window.Resources[keys[i]]=new SolidColorBrush(color);}
   Window.Resources["TextMain"]=new SolidColorBrush(Color.FromRgb(238,241,245));Window.Resources["TextMuted"]=new SolidColorBrush(Color.FromRgb(182,193,201));
   var image=C<Image>("Wallpaper");image.Source=null;
   if(appearance.Background!=""){try{image.Source=ReadWallpaper(appearance.Background);}catch(Exception ex){Status("背景无法读取，已使用配色底色："+ex.Message);}}
   if(image.Source==null)image.Source=CreateDefaultBackdrop();
   image.Effect=appearance.Glass?new BlurEffect{Radius=appearance.Blur,RenderingBias=RenderingBias.Quality}:null;
   wallpaperLabel.Text=appearance.Background==""?"当前：内置渐变背景":Path.GetFileName(appearance.Background);
  }
  DrawingImage CreateDefaultBackdrop(){var group=new DrawingGroup();var t=Themes[appearance.Theme];var brush=new LinearGradientBrush((Color)ColorConverter.ConvertFromString(t[4]),(Color)ColorConverter.ConvertFromString(t[1]),45);group.Children.Add(new GeometryDrawing(brush,null,new RectangleGeometry(new Rect(0,0,1200,800))));return new DrawingImage(group);}
 }
}
