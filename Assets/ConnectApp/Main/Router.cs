using System;
using System.Collections.Generic;
using ConnectApp.Components;
using ConnectApp.screens;
using ConnectApp.Utils;
using RSG;
using Unity.UIWidgets.animation;
using Unity.UIWidgets.async;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.ui;
using Unity.UIWidgets.widgets;
using UnityEngine;

namespace ConnectApp.Main {
    static class MainNavigatorRoutes {
        public const string Root = "/";
        public const string Splash = "/splash";
        public const string Main = "/main";
        public const string Search = "/search";
        public const string ArticleDetail = "/article-detail";
        public const string Setting = "/setting";
        public const string MyEvent = "/my-event";
        public const string MyFavorite = "/my-favorite";
        public const string History = "/history";
        public const string Login = "/login";
        public const string BindUnity = "/bind-unity";
        public const string Report = "/report";
        public const string AboutUs = "/aboutUs";
        public const string WebView = "/web-view";
        public const string UserDetail = "/user-detail";
        public const string UserFollowing = "/user-following";
        public const string UserFollower = "/user-follower";
        public const string EditPersonalInfo = "/edit-personalInfo";
        public const string PersonalRole = "/personal-role";
        public const string TeamDetail = "/team-detail";
        public const string TeamFollower = "/team-follower";
        public const string TeamMember = "/team-member";
        public const string QRScanLogin = "/qr-login";
        public const string Feedback = "/feedback";
        public const string FeedbackType = "/feedback-type";
    }

    class Router : StatelessWidget {
        static readonly GlobalKey globalKey = GlobalKey.key("main-router");
        static readonly RouteObserve<PageRoute> _routeObserve = new RouteObserve<PageRoute>();
        bool _exitApp;
        Timer _timer;

        public static NavigatorState navigator {
            get { return globalKey.currentState as NavigatorState; }
        }

        public static RouteObserve<PageRoute> routeObserve {
            get { return _routeObserve; }
        }

        static Dictionary<string, WidgetBuilder> mainRoutes {
            get {
                var routes = new Dictionary<string, WidgetBuilder> {
                    {MainNavigatorRoutes.Search, context => new SearchScreenConnector()},
                    {MainNavigatorRoutes.ArticleDetail, context => new ArticleDetailScreenConnector("")},
                    {MainNavigatorRoutes.Setting, context => new SettingScreenConnector()},
                    {MainNavigatorRoutes.MyEvent, context => new MyEventsScreenConnector()},
                    {MainNavigatorRoutes.MyFavorite, context => new MyFavoriteScreenConnector()},
                    {MainNavigatorRoutes.History, context => new HistoryScreenConnector()},
                    {MainNavigatorRoutes.Login, context => new LoginScreen()},
                    {MainNavigatorRoutes.BindUnity, context => new BindUnityScreenConnector(FromPage.setting)},
                    {MainNavigatorRoutes.Report, context => new ReportScreenConnector("", ReportType.article)},
                    {MainNavigatorRoutes.AboutUs, context => new AboutUsScreenConnector()},
                    {MainNavigatorRoutes.WebView, context => new WebViewScreen()},
                    {MainNavigatorRoutes.UserDetail, context => new UserDetailScreenConnector("")},
                    {MainNavigatorRoutes.UserFollowing, context => new UserFollowingScreenConnector("")},
                    {MainNavigatorRoutes.UserFollower, context => new UserFollowerScreenConnector("")},
                    {MainNavigatorRoutes.EditPersonalInfo, context => new EditPersonalInfoScreenConnector("")},
                    {MainNavigatorRoutes.PersonalRole, context => new PersonalJobRoleScreenConnector()},
                    {MainNavigatorRoutes.TeamDetail, context => new TeamDetailScreenConnector("")},
                    {MainNavigatorRoutes.TeamFollower, context => new TeamFollowerScreenConnector("")},
                    {MainNavigatorRoutes.TeamMember, context => new TeamMemberScreenConnector("")},
                    {MainNavigatorRoutes.QRScanLogin, context => new QRScanLoginScreenConnector("")},
                    {MainNavigatorRoutes.Feedback, context => new FeedbackScreenConnector()},
                    {MainNavigatorRoutes.FeedbackType, context => new FeedbackTypeScreenConnector()}
                };
                if (Application.isEditor) {
                    var isExistSplash = SplashManager.isExistSplash();
                    if (isExistSplash) {
                        routes.Add(key: MainNavigatorRoutes.Root, context => new SplashPage());
                        routes.Add(key: MainNavigatorRoutes.Main, context => new MainScreen());
                    }
                    else {
                        routes.Add(key: MainNavigatorRoutes.Root, context => new MainScreen());
                    }
                }
                else {
                    routes.Add(key: MainNavigatorRoutes.Splash, context => new SplashPage());
                    routes.Add(key: MainNavigatorRoutes.Main, context => new MainScreen());
                    routes.Add(key: MainNavigatorRoutes.Root, context => new RootScreen());
                }

                return routes;
            }
        }

        static Dictionary<string, WidgetBuilder> fullScreenRoutes {
            get {
                return new Dictionary<string, WidgetBuilder> {
                    {MainNavigatorRoutes.Search, context => new SearchScreenConnector()},
                    {MainNavigatorRoutes.Login, context => new LoginScreen()}
                };
            }
        }

        public override Widget build(BuildContext context) {
            GlobalContext.context = context;
            return new WillPopScope(
                onWillPop: () => {
                    var promise = new Promise<bool>();
                    if (LoginScreen.navigator?.canPop() ?? false) {
                        LoginScreen.navigator.pop();
                        promise.Resolve(false);
                    }
                    else if (Screen.orientation != ScreenOrientation.Portrait) {
                        //视频全屏时禁止物理返回按钮
                        EventBus.publish(EventBusConstant.fullScreen, new List<object> {true});
                        promise.Resolve(false);
                    }
                    else if (navigator.canPop()) {
                        navigator.pop();
                        promise.Resolve(false);
                    }
                    else {
                        if (Application.platform == RuntimePlatform.Android) {
                            if (this._exitApp) {
                                CustomToast.hidden();
                                promise.Resolve(true);
                                if (this._timer != null) {
                                    this._timer.Dispose();
                                    this._timer = null;
                                }
                            }
                            else {
                                this._exitApp = true;
                                CustomToast.show(new CustomToastItem(
                                    context: context,
                                    "再按一次退出",
                                    TimeSpan.FromMilliseconds(2000)
                                ));
                                this._timer = Window.instance.run(TimeSpan.FromMilliseconds(2000),
                                    () => { this._exitApp = false; });
                                promise.Resolve(false);
                            }
                        }
                        else {
                            promise.Resolve(true);
                        }
                    }

                    return promise;
                },
                child: new Navigator(
                    key: globalKey,
                    observers: new List<NavigatorObserver> {
                        _routeObserve
                    },
                    onGenerateRoute: settings => {
                        if (fullScreenRoutes.ContainsKey(settings.name)) {
                            return new PageRouteBuilder(
                                settings: settings,
                                (context1, animation, secondaryAnimation) => mainRoutes[settings.name](context1),
                                (context1, animation, secondaryAnimation, child) => {
                                    return new PushPageTransition(
                                        routeAnimation: animation,
                                        child: child
                                    );
                                }
                            );
                        }
                        else {
                            return new CustomPageRoute(
                                settings: settings,
                                builder: (context1) => mainRoutes[settings.name](context1)
                            );
                        }
                    }
                )
            );
        }
    }

    class PushPageTransition : StatelessWidget {
        internal PushPageTransition(
            Key key = null,
            Animation<float> routeAnimation = null, // The route's linear 0.0 - 1.0 animation.
            Widget child = null
        ) : base(key: key) {
            this._positionAnimation = this._leftPushTween.chain(this._fastOutSlowInTween).animate(routeAnimation);
            this.child = child;
        }

        readonly Tween<Offset> _leftPushTween = new OffsetTween(
            new Offset(1.0f, 0.0f),
            Offset.zero
        );

        readonly Animatable<float> _fastOutSlowInTween = new CurveTween(Curves.fastOutSlowIn);
        readonly Animation<Offset> _positionAnimation;
        readonly Widget child;

        public override Widget build(BuildContext context) {
            return new SlideTransition(
                position: this._positionAnimation,
                child: this.child
            );
        }
    }

    class ModalPageTransition : StatelessWidget {
        internal ModalPageTransition(
            Key key = null,
            Animation<float> routeAnimation = null, // The route's linear 0.0 - 1.0 animation.
            Widget child = null
        ) : base(key: key) {
            this._positionAnimation = this._bottomUpTween.chain(this._fastOutSlowInTween).animate(routeAnimation);
            this.child = child;
        }

        readonly Tween<Offset> _bottomUpTween = new OffsetTween(
            new Offset(0.0f, 1.0f),
            Offset.zero
        );

        readonly Animatable<float> _fastOutSlowInTween = new CurveTween(Curves.fastOutSlowIn);
        readonly Animation<Offset> _positionAnimation;
        readonly Widget child;

        public override Widget build(BuildContext context) {
            return new SlideTransition(
                position: this._positionAnimation,
                child: this.child
            );
        }
    }
}