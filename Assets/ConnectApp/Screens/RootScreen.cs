using System.Collections.Generic;
using ConnectApp.Components;
using ConnectApp.Constants;
using ConnectApp.Main;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.rendering;
using Unity.UIWidgets.ui;
using Unity.UIWidgets.widgets;
using UnityEngine;
using Color = Unity.UIWidgets.ui.Color;
using Image = Unity.UIWidgets.widgets.Image;

namespace ConnectApp.screens {
    public class RootScreen : StatefulWidget {
        public override State createState() {
            return new _RootScreen();
        }
    }

    class _RootScreen : State<RootScreen> {

        bool jumpToMain;
        public override void initState() {
            base.initState();
            this.jumpToMain = false;
        }
        
        

        public override Widget build(BuildContext context) {
            if (Application.platform == RuntimePlatform.Android) {
                return new Container(
                    color: new Color(0x231F20));
            }

            return new Container(
                color: CColors.White,
                child: new CustomSafeArea(
                    child: new Column(
                        mainAxisAlignment: MainAxisAlignment.end,
                        crossAxisAlignment: CrossAxisAlignment.center,
                        children: new List<Widget> {
                            new CustomButton(
                                child: new Text(
                                    "跳到主页",
                                    style: new TextStyle(
                                        height: 1.33f,
                                        fontSize: 30,
                                        fontFamily: "Roboto-Regular",
                                        color: CColors.Blue
                                    ),
                                    textAlign: TextAlign.center
                                ),
                                onPressed: () => { Router.navigator.pushNamed(MainNavigatorRoutes.Main); }
                            ),
                            new Container(
                                width: 228,
                                height: 40,
                                child: Image.asset("image/iOS/unityConnectBlack.imageset/unityConnectBlack",
                                    fit: BoxFit.cover)
                            ),
                            new Container(
                                width: 102,
                                height: 22,
                                margin: EdgeInsets.only(bottom: 16, top: 16),
                                child: Image.asset("image/iOS/madeWithUnity.imageset/madeWithUnity", fit: BoxFit.cover))
                        })
                ));
        }
    }
}