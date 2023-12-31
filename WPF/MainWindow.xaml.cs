﻿using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;
using CefSharp;
using CefSharp.Wpf;

namespace WPF;



// ReSharper disable once RedundantExtendsListEntry
public partial class MainWindow : Window {

    private static MainWindow? w;
    private static WindowChrome? windowChrome;
    private static bool browserInitialized;
    private static bool browserIsLoading = true;

    public MainWindow() {

        // CEF inicializace settings
        CefSettings s = new CefSettings();
        s.PersistSessionCookies = true;
        s.CachePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\CEF";
        s.CefCommandLineArgs.Add("disable-threaded-scrolling");
        s.CefCommandLineArgs.Add("enable-smooth-scrolling");
        s.CefCommandLineArgs.Add("enable-gpu-compositing");
        s.CefCommandLineArgs.Add("enable-gpu");
        s.CefCommandLineArgs.Add("disable-web-security", "1");
        Cef.Initialize(s);



        // inicializace mainwindow + nastavení proměnných
        InitializeComponent();
        w = this;
        windowChrome = WindowChrome.GetWindowChrome(this);



        // nastavení browseru
        var bs = new BrowserSettings();
        bs.WindowlessFrameRate = 144;
        bs.WebGl = CefState.Enabled;

        ChromiumWebBrowser.BrowserSettings = bs;
        ChromiumWebBrowser.LifeSpanHandler = new CustomLifeSpanHandler();



        // Event pro zmáčknutí tlačítka
        PreviewKeyDown += (_, e) => {
            switch (e.Key) {
                case Key.F11: WindowToggleFullscreen(); break;
                case Key.F5: ChromiumWebBrowser.Reload(); break;
            }
        };

        // event při změně sítě
        NetworkChange.NetworkAddressChanged += (_, _) => {
            CheckNetwork(true);
        };
    }





    #region Metody

        private void WindowToggleFullscreen() {

            if (w == null || windowChrome == null) {
                Console.WriteLine("WindowToggleFullscreen(): w je null");
                return;
            }

            if (w.WindowStyle != WindowStyle.SingleBorderWindow) {
                w.ResizeMode = ResizeMode.CanResize;
                w.WindowStyle = WindowStyle.SingleBorderWindow;
                w.WindowState = WindowState.Normal;
                WindowNav.Height = 30;
                w.Topmost = false;
            } else {
                w.ResizeMode = ResizeMode.NoResize;
                w.WindowStyle = WindowStyle.None;
                w.WindowState = WindowState.Maximized;
                WindowNav.Height = 0;
                w.Topmost = true;
            }

            CalcRelativeSize();
        }

        private void CalcRelativeSize() {
            StackPanel.Height = ActualHeight;
            ChromiumWebBrowser.Height = ActualHeight - WindowNav.Height;
            LoadingGrid.Height = ActualHeight - WindowNav.Height;
        }

        private async Task PlayLoadingAnimation() {
            while (true) {

                // break podmínky
                if (!browserIsLoading) {
                    Dispatcher.Invoke(() => { LoadingGridText.Text = ""; });
                    break;
                }



                if (!browserInitialized) {
                    Dispatcher.Invoke(() => {
                        if (LoadingStackPanelText.Text == "••••••") LoadingStackPanelText.Text = "";
                        LoadingStackPanelText.Text += "•";
                    });
                } else {
                    Dispatcher.Invoke(() => {
                        if (LoadingGridText.Text == "•••") LoadingGridText.Text = "";
                        LoadingGridText.Text += "•";
                    });
                }

                await Task.Delay(200);
            }
        }

        public static bool GetInternetAvailability() {
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface networkInterface in networkInterfaces) {
                if (networkInterface.OperationalStatus != OperationalStatus.Up) continue;

                if (networkInterface.NetworkInterfaceType is NetworkInterfaceType.Wireless80211 or NetworkInterfaceType.Ethernet && !networkInterface.Description.ToLowerInvariant().Contains("virtual")) {
                    return true;
                }
            }

            return false;
        }

        private async void CheckNetwork(bool executedByEvent = false) {
            await Dispatcher.Invoke(async () => {

                if (!GetInternetAvailability()) {
                    if (!browserInitialized) {
                        btnHomeControl_OnClick(null!, null!);
                        await Task.Delay(50);
                        btnBackControl_OnClick(null!, null!);
                        await Task.Delay(50);
                    }

                    OfflineModeText.Visibility = Visibility.Visible;
                    LoadingStackPanel.Visibility = Visibility.Visible;
                    LoadingStackPanelText.Text = ":(";
                    ZajimavostTextBlock.Text = "Nejsi připojený/á k internetu...";
                } else {
                    OfflineModeText.Visibility = Visibility.Collapsed;
                    LoadingStackPanelText.Text = "";
                    ZajimavostTextBlock.Text = "";

                    if(executedByEvent) btnReloadControl_OnClick(null!, null!);
                }
            });
        }
    #endregion













    #region Event Metody

        private class CustomLifeSpanHandler : ILifeSpanHandler {
            public bool DoClose(IWebBrowser chromiumWebBrowser, IBrowser browser) { return false; }

            public void OnAfterCreated(IWebBrowser chromiumWebBrowser, IBrowser browser) {}

            public void OnBeforeClose(IWebBrowser chromiumWebBrowser, IBrowser browser) {}

            public bool OnBeforePopup(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, string targetFrameName, WindowOpenDisposition targetDisposition, bool userGesture, IPopupFeatures popupFeatures, IWindowInfo windowInfo, IBrowserSettings browserSettings, ref bool noJavascriptAccess, out IWebBrowser newBrowser) {
                // Otevřít v externím Chromu, pokud je cílové chování '_blank'
                if (targetDisposition == WindowOpenDisposition.NewForegroundTab) {

                    Process p = new Process();
                    p.StartInfo.UseShellExecute = true;
                    p.StartInfo.FileName = targetUrl;
                    p.Start();

                    newBrowser = null!;
                    return true;
                }

                newBrowser = null!;
                return false;
            }
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e) {
            CalcRelativeSize();
        }

        private void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e) {
            CalcRelativeSize();
        }

        private void ZajimavostTextBlock_OnLoaded(object sender, RoutedEventArgs e) {
            string[] zajimavosti = {
                  "4Tense existuje od roku 2019, ale první video vyšlo až na začátku roku 2020",
                  "v roce 2020 náš Discord server dosáhl 12. levelu v Nitro Boostu, protože jeden člověk ukradl tátovi kreditku",
                  "prvním členem 4Tense (kromě zakladatelů) je FoFo",
                  "v době, kdy byla onlina výuka, tak byl pravidelný content a měli jsme předtočeno na 2 měsíce dopředu",
                  "4Tense se kdysi jmenovalo Soldiers of Freedom. Toto jméno vzniklo z GTA 5 crew jménem Wings of Freedom",
                  "4Tense vzniklo v době, kdy zakladatelům bylo 13 let",
                  "nejpropracovanější video na kanále 4Tense je Představení Updatu 2.3 na FreedomCRAFTu. Natáčení cinematiků zabralo celkem 8h natáčení v kuse",
                  "4Tense má i vlastní soukromý Minecraft server - FreedomCRAFT. Tento server byl spuštěn 13.11.2020",
                  "4Tense nemá grafika, tak všechnu grafiku dělá AldiiX",
                  "všichni lidi, kteří mají přístup k 4Tense channelu mají podepsanou smlouvu",
                  "FoFo byl už 2x vyhozen z 4Tense, ale vždycky se dostal zpátky",
                  "v roce 2020 jsme měli i osmihodinové streamy",
                  "v roce 2020 byl vytvořen FreedomBOT, který fungoval do 16.5. 2021. Uměl hrát hudbu, měl v sobě ekonomiku, fun commandy atd. Nyní na něm AldiiX pracuje a pracuje se na jeho comebacku",
                  "náš kanál se objevil v hodnocení kanálů u Minymuta",
                  "naše videa nejsou monetizována a všechno okolo 4Tense je uděláno zdarma",
                  "většina členů týmu má v týmu někoho, kdo bydlí ve stejném městě",
                  "první díl série Hrdinové v Overwatchi vyšel o prázdninách roku 2020",
                  "4Tense se před 14.4.2022 rok a půl jmenovalo jako The FREEDOM. Vedení jméno ale změnilo z důvodu, že se The FREEDOM (4Tense) nedalo vyhledat",
                  "díky videím o nových killerech v Dead by Daylight jsme získali okolo 150 odběratelů",
                  "první video na kanále vyšlo 22.2.2020 a bylo to video z GTA z Doomsday Heistu",
                  "od 25.5.2022 máme vlastní doménu",
                  "video „THE MASTERMIND = NOVÝ KILLER v Dead by Daylight“ natočili FoFo s AldiiXem spolu IRL",
                  "1.10. byla premiéra posledního dílu (#13) série Hrdinové v Overwatchi v Overwatch 1. Ten díl jsme hráli Torbjörna. Od dalšího dílu (#14) série je již natočena v Overwatch 2",
            };

            ZajimavostTextBlock.Text = "Víš, že " + zajimavosti[new Random().Next(0, zajimavosti.Length)] + "?";
        }

        private void btnExit_OnClick(object sender, RoutedEventArgs e) {
            Application.Current.Shutdown();
        }

        private void WindowNav_OnMouseDown(object sender, MouseButtonEventArgs e) {

            if(w == null) return;

            if (w.WindowState == WindowState.Maximized) {
                Width = ActualWidth / 1.2;
                Height = ActualHeight / 1.2;
                Point GetMousePos() => PointToScreen(Mouse.GetPosition(this));
                Top = GetMousePos().Y;

                w.ResizeMode = ResizeMode.CanResize;
                w.WindowStyle = WindowStyle.SingleBorderWindow;
                w.WindowState = WindowState.Normal;
            }



            if (Mouse.LeftButton == MouseButtonState.Pressed) w.DragMove();
        }

        private void btnMaximize_OnClick(object sender, RoutedEventArgs e) {
            if(w == null) return;

            WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;

            CalcRelativeSize();
        }

        private void btnMinimize_OnClick(object sender, RoutedEventArgs e) {
            if(w == null) return;

            WindowState = WindowState.Minimized;
        }

        private async void ChromiumWebBrowser_OnLoadingStateChanged(object? sender, LoadingStateChangedEventArgs e) {

            if (!e.IsLoading) {
                if (!browserInitialized) {
                    Dispatcher.Invoke(() => {
                        WindowNavBrowserControls.Visibility = Visibility.Visible;
                        LoadingStackPanel.Visibility = Visibility.Collapsed;
                        CheckNetwork();
                    });

                    browserInitialized = true;
                }

                if(!e.IsLoading) browserIsLoading = false;
            }

            if(browserInitialized) Dispatcher.Invoke(() => {
                LoadingGrid.Visibility = e.IsLoading ? Visibility.Visible : Visibility.Hidden;
            });

            await PlayLoadingAnimation();
            if(e.IsLoading) browserIsLoading = true;
        }

        private void btnBackControl_OnClick(object sender, RoutedEventArgs e) {
            ChromiumWebBrowser.Back();
        }

        private void btnForwardControl_OnClick(object sender, RoutedEventArgs e) {
            ChromiumWebBrowser.Forward();
        }

        private void btnReloadControl_OnClick(object sender, RoutedEventArgs e) {
            ChromiumWebBrowser.Reload();
        }

        private void btnHomeControl_OnClick(object sender, RoutedEventArgs e) {
            if(ChromiumWebBrowser.Address is "https://www.4tense.cz/#home" or "https://4tense.cz/#home") return;

            ChromiumWebBrowser.Address = "https://www.4tense.cz/#home";
        }

        private void ChromiumWebBrowser_OnLoaded(object sender, RoutedEventArgs e) {
            ChromiumWebBrowser.ExecuteScriptAsyncWhenPageLoaded(
                """
                    if(document.getElementById('FOOTER') && document.getElementById('FOOTER').getElementsByClassName('theme-switch')[0]) document.getElementById('FOOTER').getElementsByClassName('theme-switch')[0].remove();
                    if(document.getElementById("webtheme-changer")) document.getElementById("webtheme-changer").remove();
                    if(document.getElementById("crewutil-tools-parent") && document.getElementById("crewutil-tools-parent").getElementsByClassName('crewutil-tools-child')[2]) document.getElementById("crewutil-tools-parent").getElementsByClassName('crewutil-tools-child')[2].remove();
                """, false
            );
        }
    #endregion
}