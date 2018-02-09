﻿#region License
// Copyright (c) 2016-2017 Cisco Systems, Inc.

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparkNet;

namespace SparkSDK
{
    /// <summary>
    /// A Call represents a media call on Cisco Spark.
    /// The application can create an outgoing call by calling phone.dial function.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public sealed class Call
    {
        internal string CallId { get; set; }
        internal string CalleeAddress { get; set; }
        internal bool IsUsed { get; set; }
        internal bool IsGroup { get; set; }
        internal bool IsSignallingConnected { get; set; }
        internal bool IsMediaConnected { get; set; }
        internal MediaOption MediaOption { get; set; }
        internal bool IsWaittingVideoCodecActivate { get; set; }
        internal event Action<SparkApiEventArgs> AnswerCompletedHandler;

        private Phone phone;
        private SparkNet.CoreFramework m_core;
        private SparkNet.TelephonyService m_core_telephoneService;
        private bool isSendingVideo;
        private bool isSendingAudio;
        private bool isReceivingVideo;
        private bool isReceivingAudio;
        private bool isReceivingShare;
        private bool isRemoteSendingVideo;
        private bool isRemoteSendingAudio;
        private bool isRemoteSendingShare;
        private VideoDimensions localVideoViewSize;
        private VideoDimensions remoteVideoViewSize;
        private VideoDimensions remoteShareViewSize;
        private CallStatus status;
        private CallDirection direction;
        private List<CallMembership> memberships;




        internal Call(Phone phone)
        {
            this.phone = phone;
            init();
            m_core = SCFCore.Instance.m_core;
            m_core_telephoneService = SCFCore.Instance.m_core_telephoneService;
        }

        internal void init()
        {
            CallId = null;
            CalleeAddress = null;
            IsUsed = false;
            status = CallStatus.Disconnected;
            isSendingVideo = false;
            isSendingAudio = false;
            isReceivingVideo = false;
            isReceivingAudio = false;
            isRemoteSendingVideo = false;
            isRemoteSendingAudio = false;
            isReceivingShare = false;
            memberships = new List<CallMembership>();
            IsLocalRejectOrEndCall = false;
            IsGroup = false;
            IsWaittingVideoCodecActivate = false;
        }
        /// <summary>
        /// The enumeration of directions of a call
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public enum CallDirection
        {
            /// <summary>
            /// The local party is a recipient of the call.
            /// </summary>
            /// <remarks>Since: 0.1.0</remarks>
            Incoming,

            /// <summary>
            /// The local party is an initiator of the call.
            /// </summary>
            /// <remarks>Since: 0.1.0</remarks>
            Outgoing
        }

        /// <summary>
        /// video render view dimensions.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public struct VideoDimensions
        {
            /// <summary>
            /// the width of the video render view dimensions.
            /// </summary>
            /// <remarks>Since: 0.1.0</remarks>
            public uint Width;
            /// <summary>
            /// the height of video render view dimensions.
            /// </summary>
            /// <remarks>Since: 0.1.0</remarks>
            public uint Height;
        }

        /// <summary>
        /// Callback when remote participant(s) is ringing.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public event Action<Call> OnRinging;
        /// <summary>
        /// Callback when remote participant(s) answered and this call is connected.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public event Action<Call> OnConnected;
        /// <summary>
        /// Callback when this call is disconnected (hangup, cancelled, get declined or other self device pickup the call).
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public event Action<CallDisconnectedEvent> OnDisconnected;

        /// <summary>
        /// Callback when the memberships of this call have changed.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public event Action<CallMembershipChangedEvent> OnCallMembershipChanged;
        /// <summary>
        /// Callback when the media types of this call have changed.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public event Action<MediaChangedEvent> OnMediaChanged;

        /// <summary>
        /// Callback when the capabilities of this call have changed.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public event Action<Capabilities> OnCapabilitiesChanged;


        //private event Action<SparkApiEventArgs<List<ShareSource>>> SelectShareSourceCompletedHandler = null;

        /// <summary>
        /// Gets the status of this call.
        /// </summary>
        /// <value>
        /// The status. <see cref="CallStatus"/>
        /// </value>
        /// <remarks>Since: 0.1.0</remarks>
        public CallStatus Status
        {
            get { return this.status; }
            internal set
            {
                SDKLogger.Instance.Info($"status change: {Status} -> {value}");
                this.status = value;
            }
        }


        /// <summary>
        /// Gets the direction of this call.
        /// </summary>
        /// <value>
        /// The direction. <see cref="CallDirection"/>
        /// </value>
        /// <remarks>Since: 0.1.0</remarks>
        public CallDirection Direction
        {
            get { return this.direction; }
            internal set
            {
                SDKLogger.Instance.Info($"call direction is {value.ToString()}");
                this.direction = value;
            }
        }

        internal CallDisconnectedEvent ReleaseReason { get; set; }

        internal bool IsLocalRejectOrEndCall { get; set; }


        /// <summary>
        /// Gets a value indicating whether [sending DTMF enabled].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [sending DTMF enabled]; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>Since: 0.1.0</remarks>
        public bool IsSendingDTMFEnabled
        {
            get
            {
                if (CallId == null)
                {
                    SDKLogger.Instance.Error("CallId is null.");
                    return false;
                }
                return m_core_telephoneService.canSendDTMF(CallId);
            }
        }

        /// <summary>
        /// Gets a value indicating whether [remote party of this call is sending video].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [remote sending video]; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>Since: 0.1.0</remarks>
        public bool IsRemoteSendingVideo
        {
            get
            {
                return isRemoteSendingVideo;
            }
            private set { isRemoteSendingVideo = value; }
        }

        /// <summary>
        /// Gets a value indicating whether [remote party of this call is sending audio].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [remote sending audio]; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>Since: 0.1.0</remarks>
        public bool IsRemoteSendingAudio
        {
            get
            {
                return isRemoteSendingAudio;
            }
            private set { isRemoteSendingAudio = value; }
        }

        /// <summary>
        /// True if the remote party of this call is sending  share. Otherwise, false.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public bool IsRemoteSendingShare
        {
            get
            {
                return isRemoteSendingShare;
            }
            private set { isRemoteSendingShare = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [the local party of this call is sending video].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [sending video]; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>Since: 0.1.0</remarks>
        public bool IsSendingVideo
        {
            get
            {
                return isSendingVideo;
            }
            set
            {
                SDKLogger.Instance.Info($"{value}");
                m_core_telephoneService?.muteVideo(CallId, !value);
                isSendingVideo = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [the local party of this call is sending audio].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [sending audio]; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>Since: 0.1.0</remarks>
        public bool IsSendingAudio
        {
            get
            {
                return isSendingAudio;
            }
            set
            {
                SDKLogger.Instance.Info($"{value}");
                m_core_telephoneService?.muteAudio(CallId, !value);
                isSendingAudio = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [the local party of this call is receiving video].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [receiving video]; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>Since: 0.1.0</remarks>
        public bool IsReceivingVideo
        {
            get
            {
                return isReceivingVideo;
            }
            set
            {
                SDKLogger.Instance.Info($"{value}");
                m_core_telephoneService?.muteRemoteVideo(CallId, !value, TrackType.Remote);
                isReceivingVideo = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [the local party of this call is receiving audio].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [receiving audio]; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>Since: 0.1.0</remarks>
        public bool IsReceivingAudio
        {
            get
            {
                return isReceivingAudio;
            }
            set
            {
                SDKLogger.Instance.Info($"{value}");
                m_core_telephoneService?.muteRemoteAudio(CallId, !value);
                isReceivingAudio = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [the local party of this call is receiving  share].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [receiving  share]; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>Since: 0.1.0</remarks>
        public bool IsReceivingShare
        {
            get
            {
                return isReceivingShare;
            }
            set
            {
                SDKLogger.Instance.Info($"{value}");
                m_core_telephoneService?.muteRemoteVideo(CallId, !value, TrackType.RemoteShare);
                isReceivingShare = value;
            }
        }

        /// <summary>
        /// The local video render view dimensions (points) of this call.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public VideoDimensions LocalVideoViewSize
        {
            get
            {
                if (m_core_telephoneService?.getVideoSize(CallId, TrackType.Local, ref localVideoViewSize.Width, ref localVideoViewSize.Height) != true)
                {
                    SDKLogger.Instance.Error("get local video view size error.");
                }
                return localVideoViewSize;
            }
        }

        /// <summary>
        /// The remote video render view dimensions (points) of this call.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public VideoDimensions RemoteVideoViewSize
        {
            get
            {
                if (m_core_telephoneService?.getVideoSize(CallId, TrackType.Remote, ref remoteVideoViewSize.Width, ref remoteVideoViewSize.Height) != true)
                {
                    SDKLogger.Instance.Error("get remote video view size error.");
                }
                return remoteVideoViewSize;
            }
        }

        /// <summary>
        /// The remote share render view dimensions (points) of this call.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public VideoDimensions RemoteShareViewSize
        {
            get
            {
                if (m_core_telephoneService?.getVideoSize(CallId, TrackType.RemoteShare, ref remoteShareViewSize.Width, ref remoteShareViewSize.Height) != true)
                {
                    SDKLogger.Instance.Error("get remote  share view size error.");
                }
                return remoteShareViewSize;
            }
        }

        /// <summary>
        /// Gets the memberships represent participants in this call.
        /// </summary>
        /// <value>
        /// The memberships.
        /// </value>
        /// <remarks>Since: 0.1.0</remarks>
        public List<CallMembership> Memberships
        {
            get { return memberships; }
            internal set { memberships = value; }
        }


        /// <summary>
        /// Gets the initiator of this call.
        /// </summary>
        /// <value>
        /// The membership.
        /// </value>
        /// <remarks>Since: 0.1.0</remarks>
        public CallMembership From
        {
            get
            {
                return Memberships.Find(item =>
                {
                    return item.IsInitiator == true;
                });
            }
        }

        /// <summary>
        /// Get the intended recipient of this call when one on one call.
        /// </summary>
        /// <value>
        /// The membership.
        /// </value>
        /// <remarks>Since: 0.1.0</remarks>
        public CallMembership To {
            get
            {
                if (IsGroup == false)
                {
                    return Memberships.Find(item =>
                    {
                        return item.IsInitiator != true;
                    });
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Acknowledge (without answering) an incoming call.
        /// Will cause the initiator's Call instance to emit the ringing event.
        /// </summary>
        /// <param name="completedHandler">The completed event handler.</param>
        /// <remarks>Since: 0.1.0</remarks>
        public void Acknowledge(Action<SparkApiEventArgs> completedHandler)
        {
            SDKLogger.Instance.Info($"[{CallId}]");
            //scf auto return back an acknowledge message to caller.
            completedHandler?.Invoke(new SparkApiEventArgs(true, null));
        }


        /// <summary>
        /// Answers this call.
        /// This can only be invoked when this call is incoming.
        /// </summary>
        /// <param name="option">Intended media options - audio only or audio and video - for the call.</param>
        /// <param name="completedHandler">The completed event handler.</param>
        /// <remarks>Since: 0.1.0</remarks>
        public void Answer(MediaOption option, Action<SparkApiEventArgs> completedHandler)
        {
            if (Direction != CallDirection.Incoming)
            {
                SDKLogger.Instance.Error($"[{CallId}]: Failure: Unsupport function for outgoing call.");
                completedHandler?.Invoke(new SparkApiEventArgs(false, new SparkError(SparkErrorCode.IllegalOperation, "Unsupport function for outgoing call")));
            }

            if (Status > CallStatus.Ringing)
            {
                SDKLogger.Instance.Error($"[{CallId}]: Already connected, status:{Status.ToString()}");
                completedHandler?.Invoke(new SparkApiEventArgs(false, new SparkError(SparkErrorCode.IllegalStatus, "Already connected")));
            }
            SDKLogger.Instance.Info($"[{CallId}]: mediaOption:{option.MediaOptionType.ToString()}");

            MediaOption = option;

            // when video call, check if already activate the video codec license.
            // if already activated, continue call, otherwise, notify user to activate and wait the result
            if (!phone.CheckVideoCodecLicenseActivation(option))
            {
                IsWaittingVideoCodecActivate = true;
                AnswerCompletedHandler = completedHandler;
                SDKLogger.Instance.Info("video codec license hasn't activated.");

                phone.TriggerOnRequestVideoCodecActivation();
                return;
            }

            m_core_telephoneService.setMediaOption(CallId, option.MediaOptionType);
            m_core_telephoneService.setAudioMaxBandwidth(CallId, phone.AudioMaxBandwidth);
            m_core_telephoneService.setVideoMaxBandwidth(CallId, phone.VideoMaxBandwidth);
            m_core_telephoneService?.joinCall(this.CallId);

            completedHandler?.Invoke(new SparkApiEventArgs(true, null));
        }



        /// <summary>
        /// Rejects this call. 
        /// This can only be invoked when this call is incoming and in ringing status.
        /// </summary>
        /// <param name="completedHandler">The completed event handler.</param>
        /// <remarks>Since: 0.1.0</remarks>
        public void Reject(Action<SparkApiEventArgs> completedHandler)
        {
            if (Direction != CallDirection.Incoming)
            {
                SDKLogger.Instance.Error($"[{CallId}]: Unsupport function for outgoing call");
                completedHandler?.Invoke(new SparkApiEventArgs(false, new SparkError(SparkErrorCode.IllegalOperation, "Unsupport function for outgoing call")));
            }

            if (Status > CallStatus.Ringing)
            {
                SDKLogger.Instance.Error($"[{CallId}]: Already connected");
                completedHandler?.Invoke(new SparkApiEventArgs(false, new SparkError(SparkErrorCode.IllegalStatus, "Already connected")));
            }

            SDKLogger.Instance.Info($"{CallId}");
            m_core_telephoneService?.declineCall(this.CallId);

            IsLocalRejectOrEndCall = true;
            completedHandler?.Invoke(new SparkApiEventArgs(true, null));
        }

        /// <summary>
        /// Disconnects this call.
        /// This can only be invoked when this call is in answered status.
        /// </summary>
        /// <param name="completedHandler">The completed event handler.</param>
        /// <remarks>Since: 0.1.0</remarks>
        public void Hangup(Action<SparkApiEventArgs> completedHandler)
        {
            if (Status == CallStatus.Disconnected)
            {
                SDKLogger.Instance.Error($"[{CallId}]: Already disconnected");
                completedHandler?.Invoke(new SparkApiEventArgs(false, new SparkError(SparkErrorCode.IllegalStatus, "Already disconnected")));
            }

            SDKLogger.Instance.Info($"{CallId}");
            m_core_telephoneService.endCall(this.CallId);

            IsLocalRejectOrEndCall = true;
            completedHandler?.Invoke(new SparkApiEventArgs(true, null));
        }

        /// <summary>
        /// Sends feedback for this call to Cisco Spark team.
        /// </summary>
        /// <param name="rating">The rating of the quality of this call between 1 and 5 where 5 means excellent quality.</param>
        /// <param name="comments">The comments for this call.</param>
        /// <param name="includeLogs">if set to <c>true</c> [include logs], default is false.</param>
        /// <remarks>Since: 0.1.0</remarks>
        public void SendFeedbackWith(int rating, string comments = null, bool includeLogs = false)
        {
            SDKLogger.Instance.Debug($"rating[{rating}], comments[{comments}], includeLogs[{includeLogs}]");
            m_core.sendRating(rating, comments, includeLogs);
        }

        /// <summary>
        /// Sends DTMF events to the remote party. Valid DTMF events are 0-9, *, #, a-d, and A-D.
        /// </summary>
        /// <param name="dtmf">any combination of valid DTMF events matching regex mattern "^[0-9#\*abcdABCD]+$"</param>
        /// <param name="completedHandler">The completed event handler.</param>
        /// <remarks>Since: 0.1.0</remarks>
        public void SendDtmf(string dtmf, Action<SparkApiEventArgs> completedHandler)
        {
            string strValidDtmf = "";
            if (IsSendingDTMFEnabled != true)
            {
                SDKLogger.Instance.Info($"this call[{CallId}] is not support dtmf");
                completedHandler?.Invoke(new SparkApiEventArgs(false, new SparkError(SparkErrorCode.UnsupportedDTMF,"")));
                return;
            }
            SDKLogger.Instance.Info($"{CallId}");
            strValidDtmf = m_core_telephoneService.sendDTMF(this.CallId, dtmf);
            if (strValidDtmf != dtmf)
            {
                completedHandler?.Invoke(new SparkApiEventArgs(false, new SparkError(SparkErrorCode.InvalidDTMF,"")));
                return;
            }
            completedHandler?.Invoke(new SparkApiEventArgs(true, null));
        }

        /// <summary>
        /// Set remote video to display
        /// </summary>
        /// <param name="handle"> the video dispaly window handle</param>
        /// <remarks>Since: 0.1.0</remarks>
        public void SetRemoteView(IntPtr handle)
        {
            SDKLogger.Instance.Debug($"handle:{handle}");
            m_core_telephoneService.setView(this.CallId, handle, TrackType.Remote);
        }

        /// <summary>
        /// Set local view to display.
        /// </summary>
        /// <param name="handle">the local video dispaly window handle</param>
        /// <remarks>Since: 0.1.0</remarks>
        public void SetLocalView(IntPtr handle)
        {
            SDKLogger.Instance.Debug($"handle:{handle}");
            m_core_telephoneService.setView(this.CallId, handle, TrackType.Local);
        }

        /// <summary>
        /// Set local share view to display.
        /// </summary>
        /// <param name="handle">the local share dispaly window handle</param>
        /// <remarks>Since: 0.1.0</remarks>
        //public void SetLoalShareView(IntPtr handle)
        //{
        //    SDKLogger.Instance.Debug($"handle:{handle}");
        //    m_core_telephoneService.setView(this.CallId, handle, TrackType.LocalShare);
        //}

        /// <summary>
        /// Set remote share view to display.
        /// </summary>
        /// <param name="handle">the remote share dispaly window handle</param>
        /// <remarks>Since: 0.1.0</remarks>
        public void SetRemoteShareView(IntPtr handle)
        {
            SDKLogger.Instance.Debug($"handle:{handle}");
            m_core_telephoneService.setView(this.CallId, handle, TrackType.RemoteShare);
        }

        /// <summary>
        /// Update remote video to display when video window is resized.
        /// </summary>
        /// <param name="handle"> the video dispaly window handle</param>
        /// <remarks>Since: 0.1.0</remarks>
        public void UpdateRemoteView(IntPtr handle)
        {
            SDKLogger.Instance.Debug($"handle:{handle}");
            m_core_telephoneService.updateView(this.CallId, handle, TrackType.Remote);
        }

        /// <summary>
        /// Update local view to display when video window is resized.
        /// </summary>
        /// <param name="handle">the local video dispaly window handle</param>
        /// <remarks>Since: 0.1.0</remarks>
        public void UpdateLocalView(IntPtr handle)
        {
            SDKLogger.Instance.Debug($"handle:{handle}");
            m_core_telephoneService.updateView(this.CallId, handle, TrackType.Local);
        }

        /// <summary>
        /// Update local share view to display when video window is resized.
        /// </summary>
        /// <param name="handle">the local share dispaly window handle</param>
        /// <remarks>Since: 0.1.0</remarks>
        //public void UpdateLoalShareView(IntPtr handle)
        //{
        //    SDKLogger.Instance.Debug($"handle:{handle}");
        //    m_core_telephoneService.updateView(this.CallId, handle, TrackType.LocalShare);
        //}

        /// <summary>
        /// Update remote share view to display when video window is resized.
        /// </summary>
        /// <param name="handle">the remote share dispaly window handle</param>
        /// <remarks>Since: 0.1.0</remarks>
        public void UpdateRemoteShareView(IntPtr handle)
        {
            SDKLogger.Instance.Debug($"handle:{handle}");
            m_core_telephoneService.updateView(this.CallId, handle, TrackType.RemoteShare);
        }


        /// <summary>
        /// Select a kind of share source type
        /// </summary>
        /// <param name="sourceType">share source type.</param>
        /// <param name="completedHandler">The completion event handler.</param>
        /// <remarks>Since: 0.1.0</remarks>
        //public void SelectShareSource(ShareSourceType sourceType, Action<SparkApiEventArgs<List<ShareSource>>> completedHandler)
        //{
        //    if (Status != CallStatus.Connected)
        //    {
        //        completedHandler(new SparkApiEventArgs<List<ShareSource>>(false, new SparkError(SparkErrorCode.illegalOperation, "call status is not connected."), null));
        //        return;
        //    }
        //    if (sourceType != ShareSourceType.Desktop)
        //    {
        //        completedHandler(new SparkApiEventArgs<List<ShareSource>>(false, new SparkError(SparkErrorCode.illegalOperation,"now only support share desktop"), null));
        //        return;
        //    }
        //    SelectShareSourceCompletedHandler = completedHandler;
        //    m_core_telephoneService.enumerateShareSources((SparkNet.ShareSourceType)sourceType);
        //}

        /// <summary>
        /// Start share .
        /// </summary>
        /// <param name="source">the selected share source</param>
        /// <param name="completedHandler">The completed event handler.</param>
        /// <remarks>Since: 0.1.0</remarks>
        //public void StartShare(ShareSource source, Action<SparkApiEventArgs> completedHandler)
        //{
        //    if (Status != CallStatus.Connected)
        //    {
        //        SDKLogger.Instance.Error("call status is not connected.");
        //        completedHandler?.Invoke(new SparkApiEventArgs(false, new SparkError(SparkErrorCode.illegalOperation, "call status is not connected.")));
        //        return;
        //    }

        //    if (source == null || source.SourceId == null)
        //    {
        //        SDKLogger.Instance.Error("source is null or source id is null");
        //        completedHandler?.Invoke(new SparkApiEventArgs(false, new SparkError(SparkErrorCode.illegalOperation, "share soure is invalid.")));
        //        return;
        //    }
        //    SDKLogger.Instance.Debug($"{source.SourceId}");
        //    m_core_telephoneService.startShare(this.CallId, source.SourceId);
        //    completedHandler?.Invoke(new SparkApiEventArgs(true, null));
        //}

        /// <summary>
        /// Stop share .
        /// </summary>
        /// <param name="completedHandler">The completion event handler.</param>
        /// <remarks>Since: 0.1.0</remarks>
        //public void StopShare(Action<SparkApiEventArgs> completedHandler)
        //{
        //    if (Status != CallStatus.Connected)
        //    {
        //        SDKLogger.Instance.Error("call status is not connected.");
        //        completedHandler?.Invoke(new SparkApiEventArgs(false, new SparkError(SparkErrorCode.illegalOperation, "call status is not connected.")));
        //        return;
        //    }
        //    SDKLogger.Instance.Debug("");
        //    m_core_telephoneService.stopShare(this.CallId);
        //}
        internal void TrigerAnswerCompletedHandler(SparkApiEventArgs completedHandler)
        {
            AnswerCompletedHandler?.Invoke(completedHandler);
        }

        internal void TrigerOnRing()
        {
            OnRinging?.Invoke(this);
        }

        internal void TrigerOnConnected()
        {
            OnConnected?.Invoke(this);
        }

        internal void TrigerOnDisconnected(CallDisconnectedEvent reason)
        {
            OnDisconnected?.Invoke(reason);
        }

        internal void TrigerOnCapabiltyChanged(Capabilities capablity)
        {
            OnCapabilitiesChanged?.Invoke(capablity);
        }

        internal void TrigerOnMediaChanged(MediaChangedEvent mediaChangedEvent)
        {
            SDKLogger.Instance.Debug($"trigerOnMediaChanged: {mediaChangedEvent.GetType().Name}");
            if (mediaChangedEvent is LocalVideoReadyEvent)
            {
                isSendingVideo = true;
                isSendingAudio = true;
            }
            else if (mediaChangedEvent is RemoteVideoReadyEvent)
            {
                IsRemoteSendingVideo = true;
                IsRemoteSendingAudio = true;
                isReceivingVideo = true;
                isReceivingAudio = true;
            }
            else if (mediaChangedEvent is ReceivingVideoEvent)
            {
                isReceivingVideo = ((ReceivingVideoEvent)mediaChangedEvent).IsReceiving;
            }
            else if (mediaChangedEvent is ReceivingAudioEvent)
            {
                isReceivingAudio = ((ReceivingAudioEvent)mediaChangedEvent).IsReceiving;
            }
            else if (mediaChangedEvent is SendingVideoEvent)
            {

                isSendingVideo = ((SendingVideoEvent)mediaChangedEvent).IsSending;
            }
            else if (mediaChangedEvent is SendingAudioEvent)
            {
                isSendingAudio = ((SendingAudioEvent)mediaChangedEvent).IsSending;
            }
            else if (mediaChangedEvent is RemoteSendingAudioEvent)
            {
                IsRemoteSendingAudio = ((RemoteSendingAudioEvent)mediaChangedEvent).IsSending;
            }
            else if (mediaChangedEvent is RemoteSendingVideoEvent)
            {
                IsRemoteSendingVideo = ((RemoteSendingVideoEvent)mediaChangedEvent).IsSending;
            }
            else if (mediaChangedEvent is RemoteSendingShareEvent)
            {
                IsRemoteSendingShare = ((RemoteSendingShareEvent)mediaChangedEvent).IsSending;
                isReceivingShare = IsRemoteSendingShare;
            }
            else if (mediaChangedEvent is ReceivingShareEvent)
            {
                isReceivingShare = ((ReceivingShareEvent)mediaChangedEvent).IsReceiving;
            }
            else
            {

            }
            OnMediaChanged?.Invoke(mediaChangedEvent);
        }

        internal void TrigerOnCallMembershipChanged(CallMembershipChangedEvent callMembershipEvent)
        {
            SDKLogger.Instance.Info($"event[{callMembershipEvent.GetType().Name}] callmerbship[{callMembershipEvent.CallMembership.Email}]");
            OnCallMembershipChanged?.Invoke(callMembershipEvent);
        }

        //internal void TrigerOnSelectShareSource()
        //{
        //    var result = new List<ShareSource>();
        //    var shareSources = m_core_telephoneService.getShareSources();
        //    foreach (var item in shareSources)
        //    {
        //        result.Add(new ShareSource()
        //        {
        //            SourceId = item.sourceId,
        //            Name = item.name,
        //            Height = item.height,
        //            Width = item.width,
        //            X = item.x,
        //            Y = item.y,
        //            IsShared = item.shared,
        //        });
        //    }
        //    SelectShareSourceCompletedHandler?.Invoke(new SparkApiEventArgs<List<ShareSource>>(true, null, result));
        //    SelectShareSourceCompletedHandler = null;
        //}
    }
}
