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
using System.Threading.Tasks;

namespace SparkSDK
{
    /// <summary>
    /// Base class for the event of a call.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public abstract class CallEvent
    {
        /// <summary>
        /// current call instance.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        protected Call call;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallEvent"/> class.
        /// </summary>
        /// <param name="call">current call instance.</param>
        /// <remarks>Since: 0.1.0</remarks>
        protected CallEvent(Call call)
        {
            this.call = call;
        }

        /// <summary>
        /// Get current call instance.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public Call Call
        {
            get { return call; }
        }
    }

    /// <summary>
    /// The media change event.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public abstract class MediaChangedEvent : CallEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaChangedEvent"/> class.
        /// </summary>
        /// <param name="call">current call instance.</param>
        /// <remarks>Since: 0.1.0</remarks>
        protected MediaChangedEvent(Call call)
            : base(call)
        {
        }
    }

    /// <summary>
    /// The call membership changed event.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public abstract class CallMembershipChangedEvent : CallEvent
    {
        private CallMembership callMembership;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallMembershipChangedEvent"/> class.
        /// </summary>
        /// <param name="call">current call instance.</param>
        /// <param name="callMembership">This callmemership.</param>
        /// <remarks>Since: 0.1.0</remarks>
        protected CallMembershipChangedEvent(Call call, CallMembership callMembership)
            : base(call)
        {
            this.callMembership = callMembership;
        }

        /// <summary>
        /// Get this callmembership.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public CallMembership CallMembership
        {
            get { return callMembership; }
        }
    }

    /// <summary>
    /// The call disconnect event.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public abstract class CallDisconnectedEvent : CallEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CallDisconnectedEvent"/> class.
        /// </summary>
        /// <param name="call">current call instance.</param>
        /// <remarks>Since: 0.1.0</remarks>
        protected CallDisconnectedEvent(Call call)
            : base(call)
        {
        }
    }

    /// <summary>
    /// The capabilities of a call.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public abstract class Capabilities : CallEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Capabilities"/> class.
        /// </summary>
        /// <param name="call">current call instance.</param>
        /// <remarks>Since: 0.1.0</remarks>
        protected Capabilities(Call call)
            : base(call)
        {
        }
    }

    /// <summary>
    /// This is triggered when ready for show remote video. 
    /// If you haven't set the remote view handle, you can set it by calling <see cref="Call.SetRemoteView(IntPtr)"/> now.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class RemoteVideoReadyEvent : MediaChangedEvent
    {
        internal RemoteVideoReadyEvent(Call call)
            : base(call)
        {
        }
    }

    /// <summary>
    /// This is triggered when ready for show local video. 
    /// If you haven't set the local view handle, you can set it by calling <see cref="Call.SetLocalView(IntPtr)"/> now.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class LocalVideoReadyEvent : MediaChangedEvent
    {
        internal LocalVideoReadyEvent(Call call)
            : base(call)
        {
        }
    }

    /// <summary>
    /// This might be triggered when the remote party muted or unmuted the video.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class RemoteSendingVideoEvent : MediaChangedEvent
    {
        private bool isSending;

        internal RemoteSendingVideoEvent(Call call, bool sending)
            :base(call)
        {
            this.isSending = sending;
        }
        /// <summary>
        /// True if the remote party now is sending video. Otherwise false.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public bool IsSending
        {
            get { return isSending; }
        }
    }

    /// <summary>
    /// This might be triggered when the remote party muted or unmuted the audio.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class RemoteSendingAudioEvent : MediaChangedEvent
    {
        private bool isSending;

        internal RemoteSendingAudioEvent(Call call, bool sending)
            :base(call)
        {
            isSending = sending;
        }

        /// <summary>
        /// True if the remote party now is sending audio. Otherwise false.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public bool IsSending
        {
            get { return isSending; }
        }
    }

    /// <summary>
    /// This might be triggered when the local party muted or unmuted the video.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class SendingVideoEvent : MediaChangedEvent
    {
        private bool isSending;

        internal SendingVideoEvent(Call call, bool sending)
            : base(call)
        {
            isSending = sending;
        }

        /// <summary>
        /// True if the local party now is sending video. Otherwise false.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public bool IsSending
        {
            get { return isSending; }
        }
    }

    /// <summary>
    /// This might be triggered when the local party muted or unmuted the audio.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class SendingAudioEvent : MediaChangedEvent
    {
        private bool isSending;

        internal SendingAudioEvent(Call call, bool sending)
            : base(call)
        {
            isSending = sending;
        }

        /// <summary>
        /// True if the local party now is sending aduio. Otherwise false.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public bool IsSending
        {
            get { return isSending; }
        }
    }

    /// <summary>
    /// This might be triggered when the local party muted or unmuted the video.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class ReceivingVideoEvent : MediaChangedEvent
    {
        private bool isReceiving;

        internal ReceivingVideoEvent(Call call, bool receiving)
            : base(call)
        {
            isReceiving = receiving;
        }
        /// <summary>
        /// True if the local party now is receiving video. Otherwise false.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public bool IsReceiving
        {
            get { return isReceiving; }
        }
    }

    /// <summary>
    /// This might be triggered when the local party muted or unmuted the audio.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class ReceivingAudioEvent : MediaChangedEvent
    {
        private bool isReceiving;

        internal ReceivingAudioEvent(Call call, bool receiving)
            : base(call)
        {
            isReceiving = receiving;
        }
        /// <summary>
        /// True if the local party now is receiving audio. Otherwise false.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public bool IsReceiving
        {
            get { return isReceiving; }
        }
    }

    /// <summary>
    /// This might be triggered when the selected camera switched.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class CameraSwitchedEvent : MediaChangedEvent
    {
        AVIODevice camera;

        internal CameraSwitchedEvent(Call call, AVIODevice camera)
            : base(call)
        {
            this.camera = camera;
        }
        /// <summary>
        /// The selected camera.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public AVIODevice Camera
        {
            get { return camera; }
        }
    }

    /// <summary>
    /// This might be triggered when the selected speaker switched.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class SpeakerSwitchedEvent : MediaChangedEvent
    {
        AVIODevice speaker;

        internal SpeakerSwitchedEvent(Call call, AVIODevice speaker)
            : base(call)
        {
            this.speaker = speaker;
        }
        /// <summary>
        /// The selected speaker.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public AVIODevice Speaker
        {
            get { return speaker; }
        }
    }

    /// <summary>
    /// Local video rendering view size has changed.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class LocalVideoViewSizeChangedEvent : MediaChangedEvent
    {
        internal LocalVideoViewSizeChangedEvent(Call call)
            : base(call)
        {
        }
    }

    /// <summary>
    /// Remote video rendering view size has changed.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class RemoteVideoViewSizeChangedEvent : MediaChangedEvent
    {
        internal RemoteVideoViewSizeChangedEvent(Call call)
            : base(call)
        {
        }
    }

    /// <summary>
    /// Remote share rendering view size has changed.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class RemoteShareViewSizeChangedEvent : MediaChangedEvent
    {
        internal RemoteShareViewSizeChangedEvent(Call call)
            : base(call)
        {
        }
    }

    /// <summary>
    /// This might be triggered when the local party muted or unmuted the video
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class ReceivingShareEvent : MediaChangedEvent
    {
        private bool isReceiving;

        internal ReceivingShareEvent(Call call, bool receiving)
            : base(call)
        {
            isReceiving = receiving;
        }
        /// <summary>
        /// True if the local party now is receiving share. Otherwise false.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public bool IsReceiving
        {
            get { return isReceiving; }
        }
    }

    /// <summary>
    /// This might be triggered when the remote party started or stopped share stream.
    /// If you haven't set the share view handle, you can set it by calling <see cref="Call.SetRemoteShareView(IntPtr)"/> now.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class RemoteSendingShareEvent : MediaChangedEvent
    {
        private bool isSending;

        internal RemoteSendingShareEvent(Call call, bool sending)
            : base(call)
        {
            isSending = sending;
        }
        /// <summary>
        /// True if the share now is receiving. Otherwise false.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public bool IsSending
        {
            get { return isSending; }
        }
    }


    /// <summary>
    /// The person in the membership joined this call.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class CallMembershipJoinedEvent : CallMembershipChangedEvent
    {
        internal CallMembershipJoinedEvent(Call call, CallMembership callMembership)
            : base(call, callMembership)
        {
        }
    }
    /// <summary>
    /// The person in the membership left this call.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class CallMembershipLeftEvent : CallMembershipChangedEvent
    {
        internal CallMembershipLeftEvent(Call call, CallMembership callMembership)
            : base(call, callMembership)
        {
        }
    }
    /// <summary>
    /// The person in the membership declined this call.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class CallMembershipDeclinedEvent : CallMembershipChangedEvent
    {
        internal CallMembershipDeclinedEvent(Call call, CallMembership callMembership)
            : base(call, callMembership)
        {
        }
    }
    /// <summary>
    /// The person in the membership started sending video.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class CallMembershipSendingVideoEvent : CallMembershipChangedEvent
    {
        internal CallMembershipSendingVideoEvent(Call call, CallMembership callMembership)
            : base(call, callMembership)
        {
        }
    }
    /// <summary>
    /// The person in the membership started sending audio.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class CallMembershipSendingAudioEvent : CallMembershipChangedEvent
    {
        internal CallMembershipSendingAudioEvent(Call call, CallMembership callMembership)
            : base(call, callMembership)
        {
        }
    }
    /// <summary>
    /// The person in the membership started sharing.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class CallMembershipSendingShareEvent : CallMembershipChangedEvent
    {
        internal CallMembershipSendingShareEvent(Call call, CallMembership callMembership)
            : base(call, callMembership)
        {
        }
    }
    /// <summary>
    /// The local party has left the call.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class LocalLeft : CallDisconnectedEvent
    {
        internal LocalLeft(Call call)
            : base(call)
        {
        }
    }
    /// <summary>
    /// The local party has declined the call.
    /// This is only applicable when the direction of the call is *incoming*.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class LocalDecline : CallDisconnectedEvent
    {
        internal LocalDecline(Call call)
            : base(call)
        {
        }
    }
    /// <summary>
    /// The local party has cancelled the call.
    /// This is only applicable when the direction of the call is *outgoing*.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class LocalCancel : CallDisconnectedEvent
    {
        internal LocalCancel(Call call)
            : base(call)
        {
        }
    }
    /// <summary>
    /// The remote party has left the call.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class RemoteLeft : CallDisconnectedEvent
    {
        internal RemoteLeft(Call call)
            : base(call)
        {
        }
    }
    /// <summary>
    /// The remote party has declined the call.
    /// This is only applicable when the direction of the call is *outgoing*.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class RemoteDecline : CallDisconnectedEvent
    {
        internal RemoteDecline(Call call)
            : base(call)
        {
        }
    }
    /// <summary>
    /// The remote party has cancelled the call.
    /// This is only applicable when the direction of the call is *incoming*.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class RemoteCancel : CallDisconnectedEvent
    {
        internal RemoteCancel(Call call)
            : base(call)
        {
        }
    }
    /// <summary>
    /// One of the other phones of the authenticated user has answered the call.
    /// This is only applicable when the direction of the call is *incoming*.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class OtherConnected : CallDisconnectedEvent
    {
        internal OtherConnected(Call call)
            : base(call)
        {
        }
    }
    /// <summary>
    /// One of the other phones of the authenticated user has declined the call.
    /// This is only applicable when the direction of the call is *incoming*.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class OtherDeclined : CallDisconnectedEvent
    {
        internal OtherDeclined(Call call)
            : base(call)
        {
        }
    }

    /// <summary>
    /// Unknown error
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class CallError : CallDisconnectedEvent
    {
        private SparkError error;
        internal CallError(Call call, SparkError error)
            : base(call)
        {
            this.error = error;
        }
        /// <summary>
        /// <see cref="SparkError"/>
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public SparkError Error
        {
            get { return error; }
        }
    }

    /// <summary>
    /// The DTMF capability of this call.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class CapabilitieDTMF : Capabilities
    {
        private bool isEnabled;
        internal CapabilitieDTMF(Call call, bool isEnabled)
            : base(call)
        {
            this.isEnabled = isEnabled;
        }
        /// <summary>
        /// If True, this call can send and receive DTMF. Otherwise false.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public bool IsEnabled
        {
            get { return isEnabled; }
        }
    }
}
