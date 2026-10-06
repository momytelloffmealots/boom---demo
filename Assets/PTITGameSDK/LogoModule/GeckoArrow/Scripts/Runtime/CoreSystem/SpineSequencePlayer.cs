using System;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

namespace GeckoArrow.Runtime.CoreSystem
            {
                public class SpineSequencePlayer : MonoBehaviour
                {
                    [SerializeField] private List<string> spineAnimationNames;
                    [SerializeField] private SkeletonGraphic skeletonAnimation;
                    [SerializeField] private bool playOnEnable = false;
                    [SerializeField] private bool loop = false;
                    [SerializeField] private bool loopLastAnimation = true; // Loop last animation when sequence ends
                    [SerializeField] private bool disableOnComplete = false; // Disable GameObject when sequence completes
                    [SerializeField] private float timeScale = 1f;
                    [SerializeField] private float fadeTransitionDuration = 0.25f;
                        
                    private int currentIndex = 0;
                    private bool isPlaying = false;
            
                    public event Action OnSequenceCompleted;
            
                    private void Awake()
                    {
                        if (skeletonAnimation == null)
                            skeletonAnimation = GetComponent<SkeletonGraphic>();
            
                        // Set the default crossfade duration for all animations
                        skeletonAnimation.AnimationState.Data.DefaultMix = fadeTransitionDuration;
                    }
            
                    private void OnEnable()
                    {
                        if (playOnEnable)
                            PlaySequence();
                    }
            
                    private void OnDisable()
                    {
                        Stop();
                    }
            
                    public void PlaySequence()
                    {
                        if (spineAnimationNames == null || spineAnimationNames.Count == 0)
                            return;
            
                        isPlaying = true;
                        currentIndex = 0;
                        PlayCurrentAnimation();
                    }
            
                    private void PlayCurrentAnimation()
                    {
                        if (currentIndex >= spineAnimationNames.Count)
                        {
                            if (loop)
                            {
                                currentIndex = 0;
                                PlayCurrentAnimation();
                            }
                            else
                            {
                                // Complete sequence
                                OnSequenceCompleted?.Invoke();
            
                                if (loopLastAnimation && spineAnimationNames.Count > 0)
                                {
                                    currentIndex = spineAnimationNames.Count - 1;
                                    string lastAnim = spineAnimationNames[currentIndex];
                                    if (!string.IsNullOrEmpty(lastAnim))
                                    {
                                        // Unsubscribe from complete event when looping last animation
                                        skeletonAnimation.AnimationState.Complete -= OnAnimationComplete;
                                        var trackEntry = skeletonAnimation.AnimationState.SetAnimation(0, lastAnim, true); // Loop enabled
                                        trackEntry.TimeScale = timeScale;
                                    }
                                }
                                else
                                {
                                    isPlaying = false;
                                    // Disable GameObject if option is enabled
                                    if (disableOnComplete)
                                    {
                                        gameObject.SetActive(false);
                                    }
                                }
                                return;
                            }
                        }
            
                        string animName = spineAnimationNames[currentIndex];
                        if (!string.IsNullOrEmpty(animName) && skeletonAnimation != null)
                        {
                            skeletonAnimation.AnimationState.Complete -= OnAnimationComplete;
                            skeletonAnimation.AnimationState.Complete += OnAnimationComplete;
            
                            var trackEntry = skeletonAnimation.AnimationState.SetAnimation(0, animName, false);
                            trackEntry.TimeScale = timeScale;
                        }
                    }
            
                    private void OnAnimationComplete(Spine.TrackEntry trackEntry)
                    {
                        skeletonAnimation.AnimationState.Complete -= OnAnimationComplete;
                        currentIndex++;
                        PlayCurrentAnimation();
                    }
            
                    public void Stop()
                    {
                        isPlaying = false;
                        if (skeletonAnimation != null)
                            skeletonAnimation.AnimationState.Complete -= OnAnimationComplete;
                    }
            
                    public void SetLoopLastAnimation(bool shouldLoop)
                    {
                        loopLastAnimation = shouldLoop;
                    }
                    
                    public void SetDisableOnComplete(bool shouldDisable)
                    {
                        disableOnComplete = shouldDisable;
                    }
                }
            }