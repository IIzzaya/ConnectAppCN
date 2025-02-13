using System.Collections.Generic;
using ConnectApp.Components;
using ConnectApp.Components.pull_to_refresh;
using ConnectApp.Constants;
using ConnectApp.Main;
using ConnectApp.Models.ActionModel;
using ConnectApp.Models.Model;
using ConnectApp.Models.State;
using ConnectApp.Models.ViewModel;
using ConnectApp.redux.actions;
using ConnectApp.Utils;
using RSG;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.Redux;
using Unity.UIWidgets.rendering;
using Unity.UIWidgets.scheduler;
using Unity.UIWidgets.widgets;

namespace ConnectApp.screens {
    public class TeamFollowerScreenConnector : StatelessWidget {
        public TeamFollowerScreenConnector(
            string teamId,
            Key key = null
        ) : base(key: key) {
            this.teamId = teamId;
        }

        readonly string teamId;

        public override Widget build(BuildContext context) {
            return new StoreConnector<AppState, TeamFollowerScreenViewModel>(
                converter: state => {
                    var team = state.teamState.teamDict.ContainsKey(key: this.teamId)
                        ? state.teamState.teamDict[key: this.teamId]
                        : new Team();
                    var followers = team.followers ?? new List<User>();
                    var currentUserId = state.loginState.loginInfo.userId ?? "";
                    var followMap = state.followState.followDict.ContainsKey(key: currentUserId)
                        ? state.followState.followDict[key: currentUserId]
                        : new Dictionary<string, bool>();
                    return new TeamFollowerScreenViewModel {
                        teamId = this.teamId,
                        followerLoading = state.teamState.followerLoading,
                        followers = followers,
                        followersHasMore = team.followersHasMore ?? false,
                        userOffset = followers.Count,
                        userDict = state.userState.userDict,
                        userLicenseDict = state.userState.userLicenseDict,
                        followMap = followMap,
                        currentUserId = currentUserId,
                        isLoggedIn = state.loginState.isLoggedIn
                    };
                },
                builder: (context1, viewModel, dispatcher) => {
                    var actionModel = new TeamFollowerScreenActionModel {
                        startFetchFollower = () => dispatcher.dispatch(new StartFetchTeamFollowerAction()),
                        fetchFollower = offset =>
                            dispatcher.dispatch<IPromise>(Actions.fetchTeamFollower(teamId: this.teamId, offset: offset)),
                        startFollowUser = followUserId => dispatcher.dispatch(new StartFollowUserAction {
                            followUserId = followUserId
                        }),
                        followUser = followUserId =>
                            dispatcher.dispatch<IPromise>(Actions.fetchFollowUser(followUserId: followUserId)),
                        startUnFollowUser = unFollowUserId => dispatcher.dispatch(new StartUnFollowUserAction {
                            unFollowUserId = unFollowUserId
                        }),
                        unFollowUser = unFollowUserId =>
                            dispatcher.dispatch<IPromise>(Actions.fetchUnFollowUser(unFollowUserId: unFollowUserId)),
                        mainRouterPop = () => dispatcher.dispatch(new MainNavigatorPopAction()),
                        pushToLogin = () => dispatcher.dispatch(new MainNavigatorPushToAction {
                            routeName = MainNavigatorRoutes.Login
                        }),
                        pushToUserDetail = userId => dispatcher.dispatch(
                            new MainNavigatorPushToUserDetailAction {
                                userId = userId
                            }
                        )
                    };
                    return new TeamFollowerScreen(viewModel: viewModel, actionModel: actionModel);
                }
            );
        }
    }

    public class TeamFollowerScreen : StatefulWidget {
        public TeamFollowerScreen(
            TeamFollowerScreenViewModel viewModel = null,
            TeamFollowerScreenActionModel actionModel = null,
            Key key = null
        ) : base(key: key) {
            this.viewModel = viewModel;
            this.actionModel = actionModel;
        }

        public readonly TeamFollowerScreenViewModel viewModel;
        public readonly TeamFollowerScreenActionModel actionModel;

        public override State createState() {
            return new _TeamFollowerScreenState();
        }
    }

    class _TeamFollowerScreenState : State<TeamFollowerScreen>, RouteAware {
        int _userOffset;
        RefreshController _refreshController;
        string _title;

        public override void initState() {
            base.initState();
            StatusBarManager.statusBarStyle(false);
            this._userOffset = 0;
            this._refreshController = new RefreshController();
            this._title = this.widget.viewModel.currentUserId == this.widget.viewModel.teamId
                ? "我的粉丝"
                : "全部粉丝";
            SchedulerBinding.instance.addPostFrameCallback(_ => {
                this.widget.actionModel.startFetchFollower();
                this.widget.actionModel.fetchFollower(0);
            });
        }

        public override void didChangeDependencies() {
            base.didChangeDependencies();
            Router.routeObserve.subscribe(this, (PageRoute) ModalRoute.of(context: this.context));
        }

        public override void dispose() {
            Router.routeObserve.unsubscribe(this);
            base.dispose();
        }

        void _onRefresh(bool up) {
            this._userOffset = up ? 0 : this.widget.viewModel.userOffset;
            this.widget.actionModel.fetchFollower(arg: this._userOffset)
                .Then(() => this._refreshController.sendBack(up: up, up ? RefreshStatus.completed : RefreshStatus.idle))
                .Catch(_ => this._refreshController.sendBack(up: up, mode: RefreshStatus.failed));
        }

        void _onFollow(UserType userType, string userId) {
            if (this.widget.viewModel.isLoggedIn) {
                if (userType == UserType.follow) {
                    ActionSheetUtils.showModalActionSheet(
                        new ActionSheet(
                            title: "确定不再关注？",
                            items: new List<ActionSheetItem> {
                                new ActionSheetItem("确定", type: ActionType.normal,
                                    () => {
                                        this.widget.actionModel.startUnFollowUser(obj: userId);
                                        this.widget.actionModel.unFollowUser(arg: userId);
                                    }),
                                new ActionSheetItem("取消", type: ActionType.cancel)
                            }
                        )
                    );
                }

                if (userType == UserType.unFollow) {
                    this.widget.actionModel.startFollowUser(obj: userId);
                    this.widget.actionModel.followUser(arg: userId);
                }
            }
            else {
                this.widget.actionModel.pushToLogin();
            }
        }

        public override Widget build(BuildContext context) {
            var followers = this.widget.viewModel.followers;
            Widget content;
            if (this.widget.viewModel.followerLoading && followers.isEmpty()) {
                content = new GlobalLoading();
            }
            else if (followers.Count <= 0) {
                content = new BlankView(
                    $"暂无{this._title}用户",
                    "image/default-following",
                    true,
                    () => {
                        this.widget.actionModel.startFetchFollower();
                        this.widget.actionModel.fetchFollower(0);
                    }
                );
            }
            else {
                var enablePullUp = this.widget.viewModel.followersHasMore;
                content = new CustomListView(
                    controller: this._refreshController,
                    enablePullDown: true,
                    enablePullUp: enablePullUp,
                    onRefresh: this._onRefresh,
                    itemCount: followers.Count,
                    itemBuilder: this._buildUserCard,
                    footerWidget: enablePullUp ? null : CustomListViewConstant.defaultFooterWidget
                );
            }

            return new Container(
                color: CColors.White,
                child: new CustomSafeArea(
                    bottom: false,
                    child: new Container(
                        color: CColors.Background,
                        child: new Column(
                            children: new List<Widget> {
                                this._buildNavigationBar(context: context),
                                new Expanded(
                                    child: content
                                )
                            }
                        )
                    )
                )
            );
        }

        Widget _buildNavigationBar(BuildContext context) {
            return new Container(
                color: CColors.White,
                width: MediaQuery.of(context: context).size.width,
                height: 94,
                child: new Column(
                    mainAxisAlignment: MainAxisAlignment.end,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: new List<Widget> {
                        new CustomButton(
                            padding: EdgeInsets.symmetric(8, 16),
                            onPressed: () => this.widget.actionModel.mainRouterPop(),
                            child: new Icon(
                                icon: Icons.arrow_back,
                                size: 24,
                                color: CColors.Icon
                            )
                        ),
                        new Container(
                            margin: EdgeInsets.only(16, bottom: 8),
                            child: new Text(
                                data: this._title,
                                style: CTextStyle.H2
                            )
                        )
                    }
                )
            );
        }

        Widget _buildUserCard(BuildContext context, int index) {
            var follower = this.widget.viewModel.followers[index: index];
            UserType userType = UserType.unFollow;
            if (!this.widget.viewModel.isLoggedIn) {
                userType = UserType.unFollow;
            }
            else {
                var followUserLoading = false;
                if (this.widget.viewModel.userDict.ContainsKey(key: follower.id)) {
                    var user = this.widget.viewModel.userDict[key: follower.id];
                    followUserLoading = user.followUserLoading ?? false;
                }

                if (this.widget.viewModel.currentUserId == follower.id) {
                    userType = UserType.me;
                }
                else if (followUserLoading) {
                    userType = UserType.loading;
                }
                else if (this.widget.viewModel.followMap.ContainsKey(key: follower.id)) {
                    userType = UserType.follow;
                }
            }

            return new UserCard(
                user: follower,
                CCommonUtils.GetUserLicense(userId: follower.id, userLicenseMap: this.widget.viewModel.userLicenseDict),
                () => this.widget.actionModel.pushToUserDetail(obj: follower.id),
                userType: userType,
                () => this._onFollow(userType: userType, userId: follower.id),
                key: new ObjectKey(value: follower.id)
            );
        }

        public void didPopNext() {
            StatusBarManager.statusBarStyle(false);
        }

        public void didPush() {
        }

        public void didPop() {
        }

        public void didPushNext() {
        }
    }
}