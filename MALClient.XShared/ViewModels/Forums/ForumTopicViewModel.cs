﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using MALClient.Models.Enums;
using MALClient.Models.Models;
using MALClient.Models.Models.Forums;
using MALClient.XShared.Comm.MagicalRawQueries.Forums;
using MALClient.XShared.Delegates;
using MALClient.XShared.Interfaces;
using MALClient.XShared.NavArgs;
using MALClient.XShared.Utils;
using MALClient.XShared.Utils.Enums;
using MALClient.XShared.ViewModels.Forums.Items;

namespace MALClient.XShared.ViewModels.Forums
{
    public class ForumTopicViewModel : ViewModelBase , ISelfBackNavAware
    {
        public interface IScrollInfoProvider
        {
            int GetFirstVisibleItemIndex();
        }

        public event EventHandler<int> RequestScroll; 

        private bool _loadingTopic;
        

        private ForumsTopicNavigationArgs _prevArgs;
        private int _currentPage;
        private ObservableCollection<ForumTopicMessageEntryViewModel> _messages;
        private ICommand _loadPageCommand;
        private ICommand _loadGotoPageCommand;
        private ICommand _gotoLastPageCommand;
        private ICommand _gotoWebsiteCommand;
        private ICommand _gotoFirstPageCommand;
        private string _gotoPageTextBind;
        private ForumTopicData _currentTopicData;
        private ICommand _toggleWatchingCommand;
        private string _toggleWatchingButtonText;
        private ICommand _createReplyCommand;
        private string _replyMessage;
        private ICommand _navigateMessagingCommand;
        private bool _isPinned;

        public IScrollInfoProvider ScrollInfoProvider { get; set; }

        public async void Init(ForumsTopicNavigationArgs args)
        {
            ViewModelLocator.ForumsMain.CurrentBackNavRegistrar = this;
            if (LoadingTopic)
                return;

            LoadingTopic = true;
            _prevArgs = args;
            Messages?.Clear();
            AvailablePages?.Clear();

            ToggleWatchingButtonText = "Toggle watching";
            CurrentTopicData = await ForumTopicQueries.GetTopicData(_prevArgs.TopicId, _prevArgs.TopicPage, _prevArgs.LastPost, _prevArgs.MessageId);
            CurrentPage = _prevArgs.LastPost ? CurrentTopicData.AllPages : CurrentTopicData.CurrentPage;
            ViewModelLocator.GeneralMain.CurrentStatus = $"Forums - {(CurrentTopicData.IsLocked ? "Locked: " : "")}{CurrentTopicData?.Title}";
            Messages = new ObservableCollection<ForumTopicMessageEntryViewModel>(
                CurrentTopicData.Messages.Select(
                    entry => new ForumTopicMessageEntryViewModel(entry)));

            if (_prevArgs.FirstVisibleItemIndex != null)
            {
                RequestScroll?.Invoke(this,_prevArgs.FirstVisibleItemIndex.Value);
            }
            else if (_prevArgs.LastPost)
            {
                RequestScroll?.Invoke(this,Messages.Count-1);
            }
            else if (CurrentTopicData.TargetMessageId != null /*|| (_prevArgs.MessageId != null && _prevArgs.MessageId != -1)*/)
            {
                //var index = _prevArgs.MessageId != null && _prevArgs.MessageId != -1 ? _prevArgs.MessageId.ToString() : CurrentTopicData.T
                RequestScroll?.Invoke(this,Messages.IndexOf(Messages.First(model => model.Data.Id == CurrentTopicData.TargetMessageId)));
            }
            

            IsPinned = ViewModelLocator.ForumsMain.PinnedTopics.Any(entry => entry.Id == CurrentTopicData.Id);
            ViewModelLocator.ForumsMain.PinnedTopics.CollectionChanged += PinnedTopicsOnCollectionChanged;
            LoadingTopic = false;
        }

        private void PinnedTopicsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            IsPinned = ViewModelLocator.ForumsMain.PinnedTopics.Any(entry => entry.Id == CurrentTopicData.Id);
        }

        public void NavigatedFrom()
        {
            ViewModelLocator.GeneralMain.CurrentStatus = "Forums";
            ViewModelLocator.ForumsMain.PinnedTopics.CollectionChanged -= PinnedTopicsOnCollectionChanged;
        }

        public ObservableCollection<ForumTopicMessageEntryViewModel> Messages
        {
            get { return _messages; }
            set
            {
                _messages = value;
                RaisePropertyChanged(() => Messages);
            }
        }


        public ForumTopicData CurrentTopicData
        {
            get { return _currentTopicData; }
            set
            {
                _currentTopicData = value;
                RaisePropertyChanged(() => CurrentTopicData);
            }
        }

        public string Header => !LoadingTopic ? CurrentTopicData?.Title : "...";
        public bool IsLockedVisibility => !LoadingTopic && (CurrentTopicData?.IsLocked ?? false);
        public List<ForumBreadcrumb> Breadcrumbs => !LoadingTopic ? CurrentTopicData?.Breadcrumbs  : null;

        public int CurrentPage
        {
            get { return _currentPage; }
            set
            {
                _currentPage = value;
                AvailablePages.Clear();
                var start = value <= 2 ? 1 : value - 2;
                for (int i = start; i <= start + 4 && i <= CurrentTopicData.AllPages; i++)
                    AvailablePages.Add(new Tuple<int, bool>(i, i == value));

            }
        }

        public ObservableCollection<Tuple<int, bool>> AvailablePages { get; } = new ObservableCollection<Tuple<int, bool>>();


        public bool LoadingTopic
        {
            get { return _loadingTopic; }
            set
            {
                _loadingTopic = value;
                RaisePropertyChanged(() => LoadingTopic);
                RaisePropertyChanged(() => Header);
                RaisePropertyChanged(() => Breadcrumbs);
            }
        }

        public string GotoPageTextBind
        {
            get { return _gotoPageTextBind; }
            set
            {
                _gotoPageTextBind = value;
                RaisePropertyChanged(() => GotoPageTextBind);
            }
        }

        public string ToggleWatchingButtonText
        {
            get { return _toggleWatchingButtonText; }
            set
            {
                _toggleWatchingButtonText = value;
                RaisePropertyChanged(() => ToggleWatchingButtonText);
            }
        }

        public string ReplyMessage
        {
            get { return _replyMessage; }
            set
            {
                _replyMessage = value;
                RaisePropertyChanged(() => ReplyMessage);
            }
        }


        public bool IsPinned
        {
            get { return _isPinned; }
            set
            {
                _isPinned = value;
                RaisePropertyChanged(() => IsPinned);
            }
        }

        #region Commands

        public ICommand NavigateBreadcrumbsCommand => new RelayCommand<ForumBreadcrumb>(breadcrumb =>
        {
            var args = MalLinkParser.GetNavigationParametersForUrl(breadcrumb.Link);
            if (args == null)
            {
                ResourceLocator.SystemControlsLauncherService.LaunchUri(new Uri(breadcrumb.Link));
                return;
            }
            RegisterSelfBackNav();
            if (args.Item1 == PageIndex.PageAnimeDetails)
            {
                var detailsArg = args.Item2 as AnimeDetailsPageNavigationArgs;
                ViewModelLocator.GeneralMain.Navigate(PageIndex.PageForumIndex,new ForumsBoardNavigationArgs(detailsArg.Id,detailsArg.Title,detailsArg.AnimeMode));
            }
            else
            {
                ViewModelLocator.GeneralMain.Navigate(args.Item1, args.Item2);
            }

        });

        public ICommand NavigateProfileCommand => new RelayCommand<MalUser>(user =>
        {
            RegisterSelfBackNav();
            ViewModelLocator.GeneralMain.Navigate(PageIndex.PageProfile,
                new ProfilePageNavigationArgs {TargetUser = user.Name,AllowBackNavReset = false});
        });

        public ICommand LoadPageCommand => _loadPageCommand ?? (_loadPageCommand = new RelayCommand<int>(page =>
        {
            CurrentPage = page;
            LoadCurrentTopicPageAsync();
        }));

        public ICommand LoadGotoPageCommand => _loadGotoPageCommand ?? (_loadGotoPageCommand = new RelayCommand(() =>
        {
            int val;
            if (!int.TryParse(GotoPageTextBind, out val))
                return;
            CurrentPage = val;
            LoadCurrentTopicPageAsync();
            GotoPageTextBind = "";
        }));


        public ICommand GotoLastPageCommand => _gotoLastPageCommand ?? (_gotoLastPageCommand = new RelayCommand(
            () =>
            {
                CurrentPage = CurrentTopicData.AllPages;
                LoadCurrentTopicPageAsync();
            }));

        public ICommand GotoWebsiteCommand => _gotoWebsiteCommand ?? (_gotoWebsiteCommand = new RelayCommand(
            () =>
            {
                if(CurrentTopicData == null)
                    return;
                ResourceLocator.SystemControlsLauncherService.LaunchUri(new Uri($"https://myanimelist.net/forum/?topicid={CurrentTopicData.Id}"));
            }));



        public ICommand GotoFirstPageCommand => _gotoFirstPageCommand ?? (_gotoFirstPageCommand = new RelayCommand(
            () =>
            {
                CurrentPage = 1;
                LoadCurrentTopicPageAsync();
            }));

        public ICommand ToggleWatchingCommand => _toggleWatchingCommand ?? (_toggleWatchingCommand = new RelayCommand(
                                                     async () =>
                                                     {
                                                         var res =
                                                             await ForumTopicQueries.ToggleTopicWatching(
                                                                 _prevArgs.TopicId);
                                                         if(res == null)
                                                             ResourceLocator.MessageDialogProvider.ShowMessageDialog("Unable to toggle watching status.","Something went wrong");

                                                         ToggleWatchingButtonText = res == true ? "Watching" : "Stopped watching";
                                                     }));

        public ICommand CreateReplyCommand => _createReplyCommand ?? (_createReplyCommand = new RelayCommand(
                                                  async () =>
                                                  {
                                                      if(await ForumTopicQueries.CreateMessage(_prevArgs.TopicId,ReplyMessage))
                                                      {
                                                          ResourceLocator.TelemetryProvider.TelemetryTrackEvent(TelemetryTrackedEvents.CreatedReply);
                                                          ReplyMessage = string.Empty;
                                                          LoadCurrentTopicPageAsync(true,true);
                                                      }
                                                      else
                                                      {
                                                          ResourceLocator.MessageDialogProvider.ShowMessageDialog("Unable to send your reply","Something went wrong");
                                                      }
                                                  }));

        public ICommand NavigateMessagingCommand
            => _navigateMessagingCommand ?? (_navigateMessagingCommand = new RelayCommand<MalUser>(
                   user =>
                   {
                       if(ViewModelLocator.Mobile)
                           RegisterSelfBackNav();
                       ViewModelLocator.GeneralMain.Navigate(PageIndex.PageMessageDetails,new MalMessageDetailsNavArgs{WorkMode = MessageDetailsWorkMode.Message,NewMessageTarget = user.Name});
                   }));


        public ICommand PinTopicCommand
            => new RelayCommand<bool>(
                   lastpost =>
                   {
                       if(!CurrentTopicData?.Messages.Any() ?? true)
                           return;

                       var topicEntry = new ForumTopicLightEntry
                       {
                           Created = CurrentTopicData.Messages[0].CreateDate,
                           Id = CurrentTopicData.Id,
                           Lastpost = lastpost,
                           Op = CurrentTopicData.Messages[0].Poster.MalUser.Name,
                           SourceBoard = null,
                           Title = CurrentTopicData.Title
                       };
                       ViewModelLocator.ForumsMain.PinnedTopics.Add(topicEntry);

                       IsPinned = true;
                   });

        public ICommand UnpinTopicCommand
            => new RelayCommand(
                   () =>
                   {
                       if(CurrentTopicData == null)
                           return;

                       ViewModelLocator.ForumsMain.PinnedTopics.Remove(
                           ViewModelLocator.ForumsMain.PinnedTopics.First(entry => entry.Id == CurrentTopicData.Id));

                       IsPinned = false;
                   });



        public bool IsMangaBoard => _prevArgs.TopicType == TopicType.Manga;

        #endregion

        private async Task LoadCurrentTopicPageAsync(bool force = false,bool lastpost = false)
        {
            LoadingTopic = true;

            var data = await ForumTopicQueries.GetTopicData(_prevArgs.TopicId, CurrentPage,lastpost,null,force);
            if (!data.Messages.Any())
            {
                CurrentPage = CurrentTopicData.CurrentPage;
                LoadingTopic = false;
                return;
            }
            CurrentTopicData = data;
            Messages = new ObservableCollection<ForumTopicMessageEntryViewModel>(
                CurrentTopicData.Messages.Select(
                    entry => new ForumTopicMessageEntryViewModel(entry)));
            if (lastpost)
            {
                CurrentPage = CurrentTopicData.AllPages;
                RequestScroll?.Invoke(this,Messages.Count-1);
            }

            LoadingTopic = false;

        }

        public void RemoveMessage(ForumTopicMessageEntryViewModel forumTopicMessageEntryViewModel)
        {
            Messages.Remove(forumTopicMessageEntryViewModel);
            ForumTopicQueries.NotifyMessageRemoved(forumTopicMessageEntryViewModel.Data);
        }

        public async void QuouteMessage(string dataId,string poster)
        {
            ReplyMessage += $"[quote={poster} message={dataId}]" + await ForumTopicQueries.GetQuote(dataId) + "[/quote]";
        }

        public void RegisterSelfBackNav()
        {
            _prevArgs.FirstVisibleItemIndex = ScrollInfoProvider.GetFirstVisibleItemIndex();
            _prevArgs.TopicPage = CurrentPage;
            _prevArgs.MessageId = null;
            ViewModelLocator.NavMgr.RegisterBackNav(PageIndex.PageForumIndex, _prevArgs);
        }

        public async void Reload()
        {
            if(LoadingTopic)
                return;
            var message = ScrollInfoProvider.GetFirstVisibleItemIndex();
            await LoadCurrentTopicPageAsync(true);
            RequestScroll?.Invoke(this,message);
        }
    }
}
