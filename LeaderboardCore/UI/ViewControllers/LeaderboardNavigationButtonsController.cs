﻿#nullable enable
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.FloatingScreen;
using BeatSaberMarkupLanguage.ViewControllers;
using LeaderboardCore.Interfaces;
using LeaderboardCore.Models;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace LeaderboardCore.UI.ViewControllers
{
    [HotReload(RelativePathToLayout = @"..\Views\LeaderboardNavigationButtons.bsml")]
    [ViewDefinition("LeaderboardCore.UI.Views.LeaderboardNavigationButtons.bsml")]
    internal class LeaderboardNavigationButtonsController : BSMLAutomaticViewController, IInitializable, IDisposable, INotifyLeaderboardSet, INotifyScoreSaberActivate, INotifyLeaderboardLoad, INotifyCustomLeaderboardsChange
    {
        [Inject] 
        private readonly PlatformLeaderboardViewController platformLeaderboardViewController = null!;

        [InjectOptional] 
        private readonly ScoreSaberCustomLeaderboard? scoreSaberCustomLeaderboard = null!;
        
        private FloatingScreen? buttonsFloatingScreen;
        private FloatingScreen? customPanelFloatingScreen;

        private Transform? containerTransform;
        private Vector3 containerPosition;
        
        private bool leaderboardLoaded;
        private IPreviewBeatmapLevel? selectedLevel;

        private readonly List<CustomLeaderboard> customLeaderboards = new List<CustomLeaderboard>();
        private int currentIndex;
        private CustomLeaderboard? lastLeaderboard;

        public void Initialize()
        {
            buttonsFloatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(120f, 25f), false, Vector3.zero, Quaternion.identity);
            
            var buttonsFloatingScreenTransform = buttonsFloatingScreen.transform;
            buttonsFloatingScreenTransform.SetParent(platformLeaderboardViewController.transform);
            buttonsFloatingScreenTransform.localPosition = new Vector3(3f, 50f);
            buttonsFloatingScreenTransform.localScale = Vector3.one;

            var buttonsFloatingScreenGO = buttonsFloatingScreen.gameObject;
            buttonsFloatingScreenGO.SetActive(false);
            buttonsFloatingScreenGO.SetActive(true);
            buttonsFloatingScreenGO.name = "LeaderboardNavigationButtonsPanel";

            customPanelFloatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(100f, 25f), false, Vector3.zero, Quaternion.identity);

            var customFloatingScreenTransform = customPanelFloatingScreen.transform;
            customFloatingScreenTransform.SetParent(platformLeaderboardViewController.transform);
            customFloatingScreenTransform.localPosition = new Vector3(3f, 50f);
            customFloatingScreenTransform.localScale = Vector3.one;

            var customFloatingScreenGO = customPanelFloatingScreen.gameObject;
            customFloatingScreenGO.SetActive(false);
            customFloatingScreenGO.SetActive(true);
            customFloatingScreenGO.name = "CustomLeaderboardPanel";
            
            platformLeaderboardViewController.didActivateEvent += OnLeaderboardActivated;
        }

        public void Dispose()
        {
            if (buttonsFloatingScreen != null && buttonsFloatingScreen.gameObject != null)
            {
                Destroy(buttonsFloatingScreen.gameObject);
            }
            
            platformLeaderboardViewController.didActivateEvent -= OnLeaderboardActivated;
        }

        public void OnEnable()
        {
            OnLeaderboardLoaded(leaderboardLoaded);
            
            if (containerTransform == null)
            {
                containerTransform = platformLeaderboardViewController.transform.Find("Container");
                containerPosition = containerTransform.localPosition;
                OnLeaderboardLoaded(true);
            }
        }
        
        private void OnLeaderboardActivated(bool firstactivation, bool addedtohierarchy, bool screensystemenabling)
        {
            platformLeaderboardViewController.didActivateEvent -= OnLeaderboardActivated;
            buttonsFloatingScreen!.SetRootViewController(this, AnimationType.None);
        }
        
        public void OnScoreSaberActivated() => OnLeaderboardLoaded(true);

        public void OnLeaderboardSet(IDifficultyBeatmap difficultyBeatmap)
        {
            selectedLevel = difficultyBeatmap.level;
            OnLeaderboardLoaded(leaderboardLoaded);
        }

        public void OnLeaderboardLoaded(bool loaded)
        {
            leaderboardLoaded = loaded;
            var lastLeaderboardIndex = customLeaderboards.IndexOf(lastLeaderboard!);

            if (!(selectedLevel is CustomPreviewBeatmapLevel))
            {
                SwitchToDefault();
            }
            else
            {
                // If not loaded or leaderboard removed
                if (!loaded || (lastLeaderboard != null && lastLeaderboardIndex == -1 && currentIndex != 0))
                {
                    SwitchToDefault();
                }
                else if (lastLeaderboard != null && lastLeaderboardIndex != -1 && currentIndex == 0)
                {
                    SwitchToLastLeaderboard();
                }
                else if (currentIndex == 0 && !ShowDefaultLeaderboard)
                {
                    RightButtonClick();
                }   
            }

            NotifyPropertyChanged(nameof(LeftButtonActive));
            NotifyPropertyChanged(nameof(RightButtonActive));
        }

        private void SwitchToDefault()
        {
            lastLeaderboard?.Hide(customPanelFloatingScreen);
            currentIndex = 0;
            UnYeetDefault();
        }

        private void SwitchToLastLeaderboard()
        {
            if (lastLeaderboard != null)
            {
                var lastLeaderboardIndex = customLeaderboards.IndexOf(lastLeaderboard);
                lastLeaderboard.Show(customPanelFloatingScreen, containerPosition, platformLeaderboardViewController);
                currentIndex = lastLeaderboardIndex + 1;
                YeetDefault();   
            }
        }

        private void YeetDefault()
        {
            if (containerTransform != null)
            {
                containerTransform.localPosition = new Vector3(-999, -999);
            }
            scoreSaberCustomLeaderboard?.YeetSS();
        }

        private void UnYeetDefault()
        {
            if (containerTransform != null)
            {
                containerTransform.localPosition = containerPosition;
            }
            scoreSaberCustomLeaderboard?.UnYeetSS();
        }

        [UIAction("left-button-click")]
        private void LeftButtonClick()
        {
            if (lastLeaderboard == null)
            {
                return;
            }
            
            lastLeaderboard.Hide(customPanelFloatingScreen);
            currentIndex--;

            if (currentIndex == 0)
            {
                UnYeetDefault();
                lastLeaderboard = null;
            }
            else
            {
                lastLeaderboard = customLeaderboards[currentIndex - 1];
                lastLeaderboard.Show(customPanelFloatingScreen, containerPosition, platformLeaderboardViewController);
            }

            NotifyPropertyChanged(nameof(LeftButtonActive));
            NotifyPropertyChanged(nameof(RightButtonActive));
        }

        [UIAction("right-button-click")]
        private void RightButtonClick()
        {
            if (currentIndex == 0)
            {
                YeetDefault();
            }
            else
            {
                lastLeaderboard?.Hide(customPanelFloatingScreen);
            }

            currentIndex++;
            lastLeaderboard = customLeaderboards[currentIndex - 1];
            lastLeaderboard.Show(customPanelFloatingScreen, containerPosition, platformLeaderboardViewController);

            NotifyPropertyChanged(nameof(LeftButtonActive));
            NotifyPropertyChanged(nameof(RightButtonActive));
        }

        public void OnLeaderboardsChanged(List<CustomLeaderboard> customLeaderboards)
        {
            this.customLeaderboards.Clear(); 
            this.customLeaderboards.AddRange(customLeaderboards);
            
            var lastLeaderboardIndex = customLeaderboards.IndexOf(lastLeaderboard!);

            if (lastLeaderboard != null && lastLeaderboardIndex == -1 && currentIndex != 0)
            {
                SwitchToDefault();
            }
            else if (lastLeaderboard != null && lastLeaderboardIndex != -1 && currentIndex == 0)
            {
                SwitchToLastLeaderboard();
            }
            else if (currentIndex == 0 && !ShowDefaultLeaderboard)
            {
                RightButtonClick();
            }

            NotifyPropertyChanged(nameof(LeftButtonActive));
            NotifyPropertyChanged(nameof(RightButtonActive));
        }

        [UIValue("left-button-active")]
        private bool LeftButtonActive => (currentIndex > 0 && (ShowDefaultLeaderboard || currentIndex > 1 )) && leaderboardLoaded && selectedLevel is CustomPreviewBeatmapLevel;

        [UIValue("right-button-active")]
        private bool RightButtonActive => currentIndex < customLeaderboards.Count && leaderboardLoaded && selectedLevel is CustomPreviewBeatmapLevel;
        
        private bool ShowDefaultLeaderboard => scoreSaberCustomLeaderboard != null || !(selectedLevel is CustomPreviewBeatmapLevel) || customLeaderboards.Count == 0;
    }
}
