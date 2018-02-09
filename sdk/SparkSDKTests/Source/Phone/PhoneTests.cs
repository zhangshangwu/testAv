﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SparkSDK;

#region License
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
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Diagnostics;
using System.IO;
using System.Configuration;

namespace SparkSDK.Tests
{
   

    [TestClass()]
    public class PhoneTests
    {
        private static SparkTestFixture fixture;
        private static Spark spark;
        private static Person self;
        private static Phone phone;
        private static Room myRoom;
        private Call currentCall;
        private CallData callData;

        private static Process testFixtureAppProcess;
        private readonly string calleeAddress = ConfigurationManager.AppSettings["TestFixtureAppAddress01"] ?? "";
        private static readonly string testFixtureApp = "TestFixtureApp";

        

        private static bool StartTestFixtureAppProcess()
        {
            //on Jenkins, need manual start TestFixtureApp. on local, you can commit this line to auto start and close the TestFixtureApp.
            return true;
            //Console.WriteLine("start testFixtureAppProcess");
            //testFixtureAppProcess = new Process();
            //testFixtureAppProcess.StartInfo.WorkingDirectory = System.Environment.CurrentDirectory + "\\testApp";
            //testFixtureAppProcess.StartInfo.FileName = "testFixtureApp.exe";
            //bool fileExist = File.Exists(testFixtureAppProcess.StartInfo.WorkingDirectory + testFixtureAppProcess.StartInfo.FileName);
            //if (!fileExist)
            //{
            //    testFixtureAppProcess.StartInfo.WorkingDirectory = System.Environment.CurrentDirectory;
            //}
            //return testFixtureAppProcess.Start();
        }

        [ClassInitialize]
        public static void ClassSetup(TestContext context)
        {
            Console.WriteLine("ClassSetup");
            fixture = SparkTestFixture.Instance;
            Assert.IsNotNull(fixture);

            spark = fixture.CreateSparkbyJwt();
            Assert.IsNotNull(spark);

            self = GetMe();

            phone = spark.Phone;
            Assert.IsNotNull(phone);

            phone.DisableVideoCodecActivation(true);

            Assert.IsTrue(RegisterPhone());

            myRoom = CreateRoom("my test room");
            Assert.IsNotNull(myRoom);
            //Thread.Sleep(30000);//wait for scf recv conversationid

            // start testFixtureApp process
            Assert.IsTrue(StartTestFixtureAppProcess());
            //Thread.Sleep(30000);

            MessageHelper.Init();

            MessageHelper.CloseTestFixtureApp(testFixtureApp);
            Thread.Sleep(50000);

        }

        [ClassCleanup]
        public static void ClassTearDown()
        {
            Console.WriteLine("ClassTearDown");
            if (myRoom != null)
            {
                DeleteRoom(myRoom.Id);
                myRoom = null;
            }

            if (testFixtureAppProcess != null && !testFixtureAppProcess.HasExited)
            {
                Console.WriteLine("close testFixtureAppProcess");
                testFixtureAppProcess.Kill();
                testFixtureAppProcess = null;
            }

            MessageHelper.CloseTestFixtureApp(testFixtureApp);

            fixture = null;
            spark = null;
            phone = null;
            myRoom = null;
        }

        [TestInitialize]
        public void SetUp()
        {
            Console.WriteLine("set up");
            currentCall = null;
            callData = new CallData();
            Assert.IsTrue(RegisterPhone());
        }


        [TestCleanup]
        public void TearDown()
        {
            Console.WriteLine("tear down");
            if (testFixtureAppProcess != null && testFixtureAppProcess.HasExited)
            {
                Console.WriteLine("Error: testFixtureApp has exited.");
                Assert.IsTrue(StartTestFixtureAppProcess());
                Thread.Sleep(10000);
            }

            if (currentCall != null && currentCall.Status != CallStatus.Disconnected)
            {
                Console.WriteLine($"Error: call is not disconnected in the end. CallStatus:{currentCall.Status.ToString()} CallDirection:{currentCall.Direction.ToString()}");
                HangupCall(currentCall, 2000);
                MessageHelper.RunDispatcherLoop();
            }
            Thread.Sleep(10000);
        }

        [TestMethod()]
        public void RegisterTwiceThenBothSucceedTest()
        {
            Assert.IsTrue(RegisterPhone());
        }

        [TestMethod()]
        public void DeregisterTest()
        {
            Assert.IsTrue(DeregisterPhone());
        }

        [TestMethod()]
        public void DeregisterThenBothSucceedTest()
        {
            Assert.IsTrue(DeregisterPhone());
            Assert.IsTrue(DeregisterPhone());
        }

        [TestMethod()]
        public void DialAudioVideoCallAndHangUpTest()
        {
            var callee = fixture.CreatUser();
            Assert.IsNotNull(callee);

            currentCall = null;
            bool result = DialCall(callee.Email, MediaOption.AudioVideo(), ref currentCall);
            Assert.IsTrue(result);
            if (currentCall != null)
            {
                HangupCall(currentCall);
                MessageHelper.RunDispatcherLoop();
            }

            Assert.IsNotNull(currentCall);
            Assert.IsTrue(currentCall.Status >= CallStatus.Initiated);
            Assert.AreEqual(Call.CallDirection.Outgoing, currentCall.Direction);
        }

        [TestMethod()]
        public void DialAudioVideoShareCallAndHangUpTest()
        {
            var callee = fixture.CreatUser();
            Assert.IsNotNull(callee);

            currentCall = null;
            bool result = DialCall(callee.Email, MediaOption.AudioVideoShare(), ref currentCall);
            Assert.IsTrue(result);
            if (currentCall != null)
            {
                HangupCall(currentCall);
                MessageHelper.RunDispatcherLoop();
            }

            Assert.IsNotNull(currentCall);
            Assert.IsTrue(currentCall.Status >= CallStatus.Initiated);
            Assert.AreEqual(Call.CallDirection.Outgoing, currentCall.Direction);
            Assert.IsTrue(callData.ReleaseReason is LocalCancel);
        }

        [TestMethod()]
        public void DialAudioCallAndHangUpTest()
        {
            var callee = fixture.CreatUser();
            Assert.IsNotNull(callee);

            currentCall = null;
            bool result = DialCall(callee.Email, MediaOption.AudioOnly(), ref currentCall);
            Assert.IsTrue(result);
            if (currentCall != null)
            {
                HangupCall(currentCall);
                MessageHelper.RunDispatcherLoop();
            }

            Assert.IsNotNull(currentCall);
            Assert.IsTrue(currentCall.Status >= CallStatus.Initiated);
            Assert.AreEqual(Call.CallDirection.Outgoing, currentCall.Direction);
        }

        [TestMethod()]
        public void DialWithHydraPersonIdAndHangUpTest()
        {
            var callee = fixture.CreatUser();
            Assert.IsNotNull(callee);

            currentCall = null;
            bool result = DialCall(callee.PersonId, MediaOption.AudioVideoShare(), ref currentCall);
            Assert.IsTrue(result);
            if (currentCall != null)
            {
                HangupCall(currentCall);
                MessageHelper.RunDispatcherLoop();
            }

            Assert.IsNotNull(currentCall);
            Assert.IsTrue(currentCall.Status >= CallStatus.Initiated);
            Assert.AreEqual(Call.CallDirection.Outgoing, currentCall.Direction);
        }

        [TestMethod()]
        public void DialWithJwtUserAndHangUpTest()
        {
            currentCall = null;
            bool result = DialCall(calleeAddress, MediaOption.AudioVideoShare(), ref currentCall);
            Assert.IsTrue(result);
            if (currentCall != null)
            {
                HangupCall(currentCall);
                MessageHelper.RunDispatcherLoop();
            }

            Assert.IsNotNull(currentCall);
            Assert.IsTrue(currentCall.Status >= CallStatus.Initiated);
            Assert.AreEqual(Call.CallDirection.Outgoing, currentCall.Direction);
        }


        [TestMethod()]
        public void DialWithNullAddressTest()
        {
            currentCall = null;
            bool result = DialCall("", MediaOption.AudioVideoShare(), ref currentCall);
            Assert.IsFalse(result);
        }

        [TestMethod()]
        public void DialWithNullOptionTest()
        {
            currentCall = null;
            bool result = DialCall("123", null, ref currentCall);
            Assert.IsFalse(result);
        }

        [TestMethod()]
        public void DialFailWhenNotRegisterPhoneTest()
        {
            DeregisterPhone();

            var callee = fixture.CreatUser();
            Assert.IsNotNull(callee);

            currentCall = null;
            bool result = DialCall(callee.Email, MediaOption.AudioVideoShare(), ref currentCall);
            Assert.IsFalse(result);
        }

        [TestMethod()]
        public void DialFailWhenAlreadyHaveCallTest()
        {
            var callee1 = fixture.CreatUser();
            var callee2 = fixture.CreatUser();
            Assert.IsNotNull(callee1);
            Assert.IsNotNull(callee2);

            currentCall = null;
            bool result = DialCall(callee1.Email, MediaOption.AudioVideoShare(), ref currentCall);
            Assert.IsTrue(result);

            result = DialCall(callee2.Email, MediaOption.AudioVideoShare(), ref currentCall);
            Assert.IsFalse(result);

            HangupCall(currentCall);
            MessageHelper.RunDispatcherLoop();
        }

        [TestMethod()]
        public void OutgoingCallStateEventTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndHangupAfter30Seconds(testFixtureApp);

            bool ringSignal = false;
            bool connectSignal = false;
            bool disconnectSignal = false;
            currentCall = null;

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnRinging += (call) =>
                    {
                        Console.WriteLine("CurrentCall_onRinging");
                        ringSignal = true;
                    };

                    currentCall.OnConnected += (call) =>
                    {
                        Console.WriteLine("CurrentCall_onConnected");
                        connectSignal = true;
                    };

                    currentCall.OnDisconnected += (releaseReason) =>
                    {
                        Console.WriteLine("CurrentCall_onDisconnected");
                        disconnectSignal = true;
                        callData.ReleaseReason = releaseReason;
                        MessageHelper.BreakLoop();
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });
            

            MessageHelper.RunDispatcherLoop();
            Assert.IsTrue(ringSignal);
            Assert.IsTrue(connectSignal);
            Assert.IsTrue(disconnectSignal);
            Assert.IsTrue(callData.ReleaseReason is RemoteLeft);
        }

        [TestMethod()]
        public void OutgoingCallMembershipJoinedEventTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndHangupAfter30Seconds(testFixtureApp);

            currentCall = null;
            CallMembership caller = new CallMembership();
            CallMembership callee = new CallMembership();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnCallMembershipChanged += (callMembershipEvent) =>
                    {
                        Console.WriteLine($"event:{callMembershipEvent.GetType().Name}, callmembership:{callMembershipEvent.CallMembership.Email}");

                        if (callMembershipEvent is CallMembershipJoinedEvent)
                        {
                            if (self.Emails[0] == callMembershipEvent.CallMembership.Email)
                            {
                                caller = callMembershipEvent.CallMembership;
                            }
                            else
                            {
                                callee = callMembershipEvent.CallMembership;
                            }
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });


            MessageHelper.RunDispatcherLoop();

            Assert.IsTrue(caller.IsInitiator);
            Assert.AreEqual(CallMembership.CallState.Joined, caller.State);
            Assert.AreEqual(self.Id, SparkTestFixture.GetPersonIdFromUserId(caller.PersonId));
            Assert.AreEqual(self.Emails[0], caller.Email);

            Assert.IsFalse(callee.IsInitiator);
            Assert.AreEqual(CallMembership.CallState.Joined, callee.State);
            Assert.AreEqual(calleeAddress, callee.Email);            
        }

        [TestMethod()]
        public void OutgoingCallMembershipLeftEventTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndHangupAfter30Seconds(testFixtureApp);

            currentCall = null;
            CallMembership caller = new CallMembership();
            CallMembership callee = new CallMembership();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnCallMembershipChanged += (callMembershipEvent) =>
                    {
                        Console.WriteLine($"event:{callMembershipEvent.GetType().Name}, callmembership:{callMembershipEvent.CallMembership.Email}");

                        if (callMembershipEvent is CallMembershipLeftEvent)
                        {
                            if (self.Emails[0] == callMembershipEvent.CallMembership.Email)
                            {
                                caller = callMembershipEvent.CallMembership;
                            }
                            else
                            {
                                callee = callMembershipEvent.CallMembership;
                            }
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });


            MessageHelper.RunDispatcherLoop();

            Assert.IsTrue(caller.IsInitiator);
            Assert.AreEqual(CallMembership.CallState.Left, caller.State);
            Assert.AreEqual(self.Id, SparkTestFixture.GetPersonIdFromUserId(caller.PersonId));
            Assert.AreEqual(self.Emails[0], caller.Email);

            Assert.IsFalse(callee.IsInitiator);
            Assert.AreEqual(CallMembership.CallState.Left, callee.State);
            Assert.AreEqual(calleeAddress, callee.Email);
        }

        [TestMethod()]
        public void OutgoingCallMembershipEventDeclineTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: decline
            MessageHelper.SetTestMode_CalleeAutoDecline(testFixtureApp);

            currentCall = null;
            CallMembership callee = new CallMembership();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (releaseReason) =>
                    {
                        Console.WriteLine("onDisconnected");
                        callData.ReleaseReason = releaseReason;
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnCallMembershipChanged += (callMembershipEvent) =>
                    {
                        Console.WriteLine($"event:{callMembershipEvent.GetType().Name}, callmembership:{callMembershipEvent.CallMembership.Email}");

                        if (callMembershipEvent is CallMembershipDeclinedEvent)
                        {
                            callee = callMembershipEvent.CallMembership;
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });


            MessageHelper.RunDispatcherLoop();

            Assert.IsFalse(callee.IsInitiator);
            Assert.AreEqual(calleeAddress, callee.Email);
            Assert.AreEqual(CallMembership.CallState.Declined, callee.State);
            Assert.IsTrue(callData.ReleaseReason is RemoteDecline);
        }

        [TestMethod()]
        public void OutgoingCallMembershipSendingVideoEventMuteTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. callee: mute video
            //4. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndMuteVideoAndHangupAfter30Seconds(testFixtureApp);

            currentCall = null;
            List<CallMembership> callee = new List<CallMembership>();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnCallMembershipChanged += (callMembershipEvent) =>
                    {
                        Console.WriteLine($"event:{callMembershipEvent.GetType().Name}, callmembership:{callMembershipEvent.CallMembership.Email}");

                        if (callMembershipEvent is CallMembershipSendingVideoEvent)
                        {
                            callee.Add(callMembershipEvent.CallMembership);
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });


            MessageHelper.RunDispatcherLoop();
            Assert.IsTrue(callee.Count > 0);
            Assert.AreEqual(calleeAddress, callee[0].Email);
            Assert.IsFalse(callee[0].IsSendingVideo);
        }

        [TestMethod()]
        public void OutgoingCallMembershipSendingVideoEventUnmuteTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. callee: mute video
            //4. callee: unmte video
            //5. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndMuteVideoAndUnMuteVideoAndHangupAfter30Seconds(testFixtureApp);

            currentCall = null;
            List<CallMembership> callee = new List<CallMembership>();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnCallMembershipChanged += (callMembershipEvent) =>
                    {
                        Console.WriteLine($"event:{callMembershipEvent.GetType().Name}, callmembership:{callMembershipEvent.CallMembership.Email}");

                        if (callMembershipEvent is CallMembershipSendingVideoEvent)
                        {
                            callee.Add(callMembershipEvent.CallMembership);
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });


            MessageHelper.RunDispatcherLoop();

            Assert.AreEqual(2, callee.Count);
            Assert.AreEqual(calleeAddress, callee[0].Email);
            Assert.IsFalse(callee[0].IsSendingVideo);
            Assert.AreEqual(calleeAddress, callee[1].Email);
            Assert.IsTrue(callee[1].IsSendingVideo);
        }

        [TestMethod()]
        public void OutgoingCallMembershipSendingAudioEventMuteTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. callee: mute audio
            //4. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndMuteAudioAndHangupAfter30Seconds(testFixtureApp);

            currentCall = null;
            List<CallMembership> callee = new List<CallMembership>();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnCallMembershipChanged += (callMembershipEvent) =>
                    {
                        Console.WriteLine($"event:{callMembershipEvent.GetType().Name}, callmembership:{callMembershipEvent.CallMembership.Email}");

                        if (callMembershipEvent is CallMembershipSendingAudioEvent)
                        {
                            callee.Add(callMembershipEvent.CallMembership);
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });


            MessageHelper.RunDispatcherLoop();

            Assert.IsTrue(callee.Count > 0);
            Assert.AreEqual(calleeAddress, callee[0].Email);
            Assert.IsFalse(callee[0].IsSendingAudio);
        }

        [TestMethod()]
        public void OutgoingCallMembershipSendingAudioEventUnMuteTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. callee: mute audio
            //4. callee: unmute audio
            //5. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndMuteAudioAndUnMuteAudioAndHangupAfter30Seconds(testFixtureApp);

            currentCall = null;
            List<CallMembership> callee = new List<CallMembership>();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnCallMembershipChanged += (callMembershipEvent) =>
                    {
                        Console.WriteLine($"event:{callMembershipEvent.GetType().Name}, callmembership:{callMembershipEvent.CallMembership.Email}");

                        if (callMembershipEvent is CallMembershipSendingAudioEvent)
                        {
                            callee.Add(callMembershipEvent.CallMembership);
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });


            MessageHelper.RunDispatcherLoop();

            Assert.AreEqual(2, callee.Count);
            Assert.AreEqual(calleeAddress, callee[0].Email);
            Assert.IsFalse(callee[0].IsSendingAudio);
            Assert.AreEqual(calleeAddress, callee[1].Email);
            Assert.IsTrue(callee[1].IsSendingAudio);
        }

        [TestMethod()]
        public void OutgoingCallMembershipSendingShareEventStartTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. callee: start share
            //4. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndStartShareAndHangupAfter30s(testFixtureApp);

            currentCall = null;
            List<CallMembership> callee = new List<CallMembership>();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnCallMembershipChanged += (callMembershipEvent) =>
                    {
                        Console.WriteLine($"event:{callMembershipEvent.GetType().Name}, callmembership:{callMembershipEvent.CallMembership.Email}");

                        if (callMembershipEvent is CallMembershipSendingShareEvent)
                        {
                            callee.Add(callMembershipEvent.CallMembership);
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });


            MessageHelper.RunDispatcherLoop();

            Assert.IsTrue(callee.Count > 0);
            Assert.AreEqual(calleeAddress, callee[0].Email);
            Assert.IsTrue(callee[0].IsSendingShare);
        }


        [TestMethod()]
        public void OutgoingCallMembershipSendingShareEventStopTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. callee: start share
            //4. callee: stop share
            //5. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndStartShare15sAndHangupAfter30s(testFixtureApp);

            currentCall = null;
            List<CallMembership> callee = new List<CallMembership>();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnCallMembershipChanged += (callMembershipEvent) =>
                    {
                        Console.WriteLine($"event:{callMembershipEvent.GetType().Name}, callmembership:{callMembershipEvent.CallMembership.Email}");

                        if (callMembershipEvent is CallMembershipSendingShareEvent)
                        {
                            callee.Add(callMembershipEvent.CallMembership);
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });

            MessageHelper.RunDispatcherLoop();

            Assert.AreEqual(2, callee.Count);
            Assert.AreEqual(calleeAddress, callee[0].Email);
            Assert.IsTrue(callee[0].IsSendingShare);
            Assert.AreEqual(calleeAddress, callee[1].Email);
            Assert.IsFalse(callee[1].IsSendingShare);
        }


        [TestMethod()]
        public void OutgoingMediaChangedVideoReadyVideoSizeChangedEventTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndHangupAfter30Seconds(testFixtureApp);

            currentCall = null;
            List<MediaChangedEvent> mediaEvents = new List<MediaChangedEvent>();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnMediaChanged += (callMediaChangedEvent) =>
                    {
                        Console.WriteLine($"event:{callMediaChangedEvent.GetType().Name}");
                        mediaEvents.Add(callMediaChangedEvent);
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });

            MessageHelper.RunDispatcherLoop();

            Assert.IsTrue(mediaEvents.Count > 0);
            Assert.IsTrue(mediaEvents[0] is LocalVideoReadyEvent);
            Assert.IsTrue(mediaEvents[1] is LocalVideoViewSizeChangedEvent);
            Assert.IsTrue(mediaEvents[2] is RemoteVideoReadyEvent);
            //Assert.IsTrue(mediaEvents[3] is RemoteVideoViewSizeChangedEvent);
        }


        [TestMethod()]
        public void OutgoingMediaChangedRemoteSendingVideoEventMuteTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. callee: mute video
            //4. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndMuteVideoAndHangupAfter30Seconds(testFixtureApp);

            currentCall = null;
            List<MediaChangedEvent> mediaEvents = new List<MediaChangedEvent>();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnMediaChanged += (callMediaChangedEvent) =>
                    {
                        Console.WriteLine($"event:{callMediaChangedEvent.GetType().Name}");
                        if (callMediaChangedEvent is RemoteSendingVideoEvent)
                        {
                            mediaEvents.Add(callMediaChangedEvent);
                            callData.listIsRemoteSendingVideo.Add(currentCall.IsRemoteSendingVideo);
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });

            MessageHelper.RunDispatcherLoop();

            Assert.IsTrue(mediaEvents.Count > 0);
            var mediaevent = mediaEvents[0] as RemoteSendingVideoEvent;
            Assert.IsNotNull(mediaevent);
            Assert.IsFalse(mediaevent.IsSending);
            Assert.IsTrue(callData.listIsRemoteSendingVideo.Count > 0);
            Assert.IsFalse(callData.listIsRemoteSendingVideo[0]);
        }

        [TestMethod()]
        public void OutgoingMediaChangedRemoteSendingVideoEventUnMuteTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. callee: mute video
            //4. callee: unmute video
            //4. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndMuteVideoAndUnMuteVideoAndHangupAfter30Seconds(testFixtureApp);

            currentCall = null;
            List<MediaChangedEvent> mediaEvents = new List<MediaChangedEvent>();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnMediaChanged += (callMediaChangedEvent) =>
                    {
                        Console.WriteLine($"event:{callMediaChangedEvent.GetType().Name}");
                        if (callMediaChangedEvent is RemoteSendingVideoEvent)
                        {
                            mediaEvents.Add(callMediaChangedEvent);
                            callData.listIsRemoteSendingVideo.Add(currentCall.IsRemoteSendingVideo);
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });

            MessageHelper.RunDispatcherLoop();

            Assert.AreEqual(2, mediaEvents.Count);
            var mediaevent = mediaEvents[0] as RemoteSendingVideoEvent;
            Assert.IsNotNull(mediaevent);
            Assert.IsFalse(mediaevent.IsSending);
            mediaevent = mediaEvents[1] as RemoteSendingVideoEvent;
            Assert.IsNotNull(mediaevent);
            Assert.IsTrue(mediaevent.IsSending);
            Assert.AreEqual(2, callData.listIsRemoteSendingVideo.Count);
            Assert.IsFalse(callData.listIsRemoteSendingVideo[0]);
            Assert.IsTrue(callData.listIsRemoteSendingVideo[1]);
        }

        [TestMethod()]
        public void OutgoingMediaChangedRemoteSendingAudioEventMuteTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. callee: mute audio
            //4. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndMuteAudioAndHangupAfter30Seconds(testFixtureApp);

            currentCall = null;
            List<MediaChangedEvent> mediaEvents = new List<MediaChangedEvent>();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnMediaChanged += (callMediaChangedEvent) =>
                    {
                        Console.WriteLine($"event:{callMediaChangedEvent.GetType().Name}");
                        if (callMediaChangedEvent is RemoteSendingAudioEvent)
                        {
                            mediaEvents.Add(callMediaChangedEvent);
                            callData.listIsRemoteSendingAudio.Add(currentCall.IsRemoteSendingAudio);
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });

            MessageHelper.RunDispatcherLoop();

            Assert.IsTrue(mediaEvents.Count > 0);
            var mediaevent = mediaEvents[0] as RemoteSendingAudioEvent;
            Assert.IsNotNull(mediaevent);
            Assert.IsFalse(mediaevent.IsSending);
            Assert.IsTrue(callData.listIsRemoteSendingAudio.Count > 0);
            Assert.IsFalse(callData.listIsRemoteSendingAudio[0]);
        }


        [TestMethod()]
        public void OutgoingMediaChangedRemoteSendingAudioEventUnMuteTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. callee: mute audio
            //4. callee: unmute audio
            //4. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndMuteAudioAndUnMuteAudioAndHangupAfter30Seconds(testFixtureApp);

            currentCall = null;
            List<MediaChangedEvent> mediaEvents = new List<MediaChangedEvent>();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnMediaChanged += (callMediaChangedEvent) =>
                    {
                        Console.WriteLine($"event:{callMediaChangedEvent.GetType().Name}");
                        if (callMediaChangedEvent is RemoteSendingAudioEvent)
                        {
                            mediaEvents.Add(callMediaChangedEvent);
                            callData.listIsRemoteSendingAudio.Add(currentCall.IsRemoteSendingAudio);
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });

            MessageHelper.RunDispatcherLoop();

            Assert.AreEqual(2, mediaEvents.Count);
            var mediaevent = mediaEvents[0] as RemoteSendingAudioEvent;
            Assert.IsNotNull(mediaevent);
            Assert.IsFalse(mediaevent.IsSending);
            mediaevent = mediaEvents[1] as RemoteSendingAudioEvent;
            Assert.IsNotNull(mediaevent);
            Assert.IsTrue(mediaevent.IsSending);
            Assert.AreEqual(2, callData.listIsRemoteSendingAudio.Count);
            Assert.IsFalse(callData.listIsRemoteSendingAudio[0]);
            Assert.IsTrue(callData.listIsRemoteSendingAudio[1]);
        }

        [TestMethod()]
        public void OutgoingMediaChangedSendingVideoEventMuteTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. caller: mute video
            //4. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndHangupAfter30Seconds(testFixtureApp);

            currentCall = null;
            List<MediaChangedEvent> mediaEvents = new List<MediaChangedEvent>();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnConnected += (call) =>
                    {
                        Console.WriteLine("onConnected");
                        Console.WriteLine("local mute video");
                        currentCall.IsSendingVideo = false;
                    };
                    currentCall.OnMediaChanged += (callMediaChangedEvent) =>
                    {
                        Console.WriteLine($"event:{callMediaChangedEvent.GetType().Name}");
                        if (callMediaChangedEvent is SendingVideoEvent)
                        {
                            mediaEvents.Add(callMediaChangedEvent);
                            callData.listIsSendingVideo.Add(currentCall.IsSendingVideo);
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });

            MessageHelper.RunDispatcherLoop();

            Assert.IsTrue(mediaEvents.Count > 0);
            var mediaevent = mediaEvents[0] as SendingVideoEvent;
            Assert.IsNotNull(mediaevent);
            Assert.IsFalse(mediaevent.IsSending);
            Assert.IsTrue(callData.listIsSendingVideo.Count > 0);
            Assert.IsFalse(callData.listIsSendingVideo[0]);
        }

        [TestMethod()]
        public void OutgoingMediaChangedSendingVideoEventUnMuteTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. caller: mute video
            //4. caller: unmute video
            //4. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndHangupAfter30Seconds(testFixtureApp);

            currentCall = null;
            List<MediaChangedEvent> mediaEvents = new List<MediaChangedEvent>();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnConnected += (call) =>
                    {
                        Console.WriteLine("onConnected");
                        Console.WriteLine("local mute video");
                        currentCall.IsSendingVideo = false;
                    };
                    currentCall.OnMediaChanged += (callMediaChangedEvent) =>
                    {
                        Console.WriteLine($"event:{callMediaChangedEvent.GetType().Name}");
                        if (callMediaChangedEvent is SendingVideoEvent)
                        {
                            mediaEvents.Add(callMediaChangedEvent);
                            callData.listIsSendingVideo.Add(currentCall.IsSendingVideo);

                            if (((SendingVideoEvent)callMediaChangedEvent).IsSending == false)
                            {
                                Console.WriteLine("local unmute video");
                                currentCall.IsSendingVideo = true;
                            }
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });

            MessageHelper.RunDispatcherLoop();

            Assert.AreEqual(2, mediaEvents.Count);
            var mediaevent = mediaEvents[0] as SendingVideoEvent;
            Assert.IsNotNull(mediaevent);
            Assert.IsFalse(mediaevent.IsSending);
            mediaevent = mediaEvents[1] as SendingVideoEvent;
            Assert.IsNotNull(mediaevent);
            Assert.IsTrue(mediaevent.IsSending);
            Assert.AreEqual(2, callData.listIsSendingVideo.Count);
            Assert.IsFalse(callData.listIsSendingVideo[0]);
            Assert.IsTrue(callData.listIsSendingVideo[1]);
        }


        [TestMethod()]
        public void OutgoingMediaChangedSendingAudioEventMuteTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. caller: mute audio
            //4. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndHangupAfter30Seconds(testFixtureApp);

            currentCall = null;
            List<MediaChangedEvent> mediaEvents = new List<MediaChangedEvent>();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnConnected += (call) =>
                    {
                        Console.WriteLine("onConnected");
                        Console.WriteLine("local mute audio");
                        currentCall.IsSendingAudio = false;
                    };
                    currentCall.OnMediaChanged += (callMediaChangedEvent) =>
                    {
                        Console.WriteLine($"event:{callMediaChangedEvent.GetType().Name}");
                        if (callMediaChangedEvent is SendingAudioEvent)
                        {
                            mediaEvents.Add(callMediaChangedEvent);
                            callData.listIsSendingAudio.Add(currentCall.IsSendingAudio);
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });

            MessageHelper.RunDispatcherLoop();

            Assert.IsTrue(mediaEvents.Count > 0);
            var mediaevent = mediaEvents[0] as SendingAudioEvent;
            Assert.IsNotNull(mediaevent);
            Assert.IsFalse(mediaevent.IsSending);
            Assert.IsTrue(callData.listIsSendingAudio.Count > 0);
            Assert.IsFalse(callData.listIsSendingAudio[0]);
        }

        [TestMethod()]
        public void OutgoingMediaChangedSendingAudioEventUnMuteTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. caller: mute audio
            //4. caller: unmute audio
            //4. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndHangupAfter30Seconds(testFixtureApp);

            currentCall = null;
            List<MediaChangedEvent> mediaEvents = new List<MediaChangedEvent>();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnConnected += (call) =>
                    {
                        Console.WriteLine("onConnected");
                        Console.WriteLine("local mute audio");
                        currentCall.IsSendingAudio = false;
                    };
                    currentCall.OnMediaChanged += (callMediaChangedEvent) =>
                    {
                        Console.WriteLine($"event:{callMediaChangedEvent.GetType().Name}");
                        if (callMediaChangedEvent is SendingAudioEvent)
                        {
                            mediaEvents.Add(callMediaChangedEvent);
                            callData.listIsSendingAudio.Add(currentCall.IsSendingAudio);

                            if (((SendingAudioEvent)callMediaChangedEvent).IsSending == false)
                            {
                                Console.WriteLine("local unmute audio");
                                currentCall.IsSendingAudio = true;
                            }
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });

            MessageHelper.RunDispatcherLoop();

            Assert.AreEqual(2, mediaEvents.Count);
            var mediaevent = mediaEvents[0] as SendingAudioEvent;
            Assert.IsNotNull(mediaevent);
            Assert.IsFalse(mediaevent.IsSending);
            mediaevent = mediaEvents[1] as SendingAudioEvent;
            Assert.IsNotNull(mediaevent);
            Assert.IsTrue(mediaevent.IsSending);
            Assert.AreEqual(2, callData.listIsSendingAudio.Count);
            Assert.IsFalse(callData.listIsSendingAudio[0]);
            Assert.IsTrue(callData.listIsSendingAudio[1]);
        }

        [TestMethod()]
        public void OutgoingMediaChangedReceivingVideoEventMuteTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. caller: mute remote video
            //4. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndHangupAfter30Seconds(testFixtureApp);

            currentCall = null;
            List<MediaChangedEvent> mediaEvents = new List<MediaChangedEvent>();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnConnected += (call) =>
                    {
                        Console.WriteLine("onConnected");
                        Console.WriteLine("local mute remote video");
                        currentCall.IsReceivingVideo = false;
                    };
                    currentCall.OnMediaChanged += (callMediaChangedEvent) =>
                    {
                        Console.WriteLine($"event:{callMediaChangedEvent.GetType().Name}");
                        if (callMediaChangedEvent is ReceivingVideoEvent)
                        {
                            mediaEvents.Add(callMediaChangedEvent);
                            callData.listIsReceivingVideo.Add(currentCall.IsReceivingVideo);
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });

            MessageHelper.RunDispatcherLoop();

            Assert.AreEqual(1, mediaEvents.Count);
            var mediaevent = mediaEvents[0] as ReceivingVideoEvent;
            Assert.IsNotNull(mediaevent);
            Assert.IsFalse(mediaevent.IsReceiving);
            Assert.IsTrue(callData.listIsReceivingVideo.Count > 0);
            Assert.IsFalse(callData.listIsReceivingVideo[0]);
        }

        [TestMethod()]
        public void OutgoingMediaChangedReceivingVideoEventUnMuteTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. caller: mute remote video
            //4. caller: unmute remote video
            //5. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndHangupAfter30Seconds(testFixtureApp);

            currentCall = null;
            List<MediaChangedEvent> mediaEvents = new List<MediaChangedEvent>();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnConnected += (call) =>
                    {
                        Console.WriteLine("onConnected");
                        Console.WriteLine("local mute remote video");
                        currentCall.IsReceivingVideo = false;
                    };
                    currentCall.OnMediaChanged += (callMediaChangedEvent) =>
                    {
                        Console.WriteLine($"event:{callMediaChangedEvent.GetType().Name}");
                        if (callMediaChangedEvent is ReceivingVideoEvent)
                        {
                            mediaEvents.Add(callMediaChangedEvent);
                            callData.listIsReceivingVideo.Add(currentCall.IsReceivingVideo);

                            if (((ReceivingVideoEvent)callMediaChangedEvent).IsReceiving == false)
                            {
                                Console.WriteLine("local unmute remote video");
                                currentCall.IsReceivingVideo = true;
                            }
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });

            MessageHelper.RunDispatcherLoop();

            Assert.AreEqual(2, mediaEvents.Count);
            var mediaevent = mediaEvents[0] as ReceivingVideoEvent;
            Assert.IsNotNull(mediaevent);
            Assert.IsFalse(mediaevent.IsReceiving);
            mediaevent = mediaEvents[1] as ReceivingVideoEvent;
            Assert.IsNotNull(mediaevent);
            Assert.IsTrue(mediaevent.IsReceiving);
            Assert.AreEqual(2, callData.listIsReceivingVideo.Count);
            Assert.IsFalse(callData.listIsReceivingVideo[0]);
            Assert.IsTrue(callData.listIsReceivingVideo[1]);
        }

        [TestMethod()]
        public void OutgoingMediaChangedReceivingAudioEventMuteTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. caller: mute remote audio
            //4. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndHangupAfter30Seconds(testFixtureApp);

            currentCall = null;
            List<MediaChangedEvent> mediaEvents = new List<MediaChangedEvent>();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnConnected += (call) =>
                    {
                        Console.WriteLine("onConnected");
                        Console.WriteLine("local mute remote audio");
                        currentCall.IsReceivingAudio = false;
                    };
                    currentCall.OnMediaChanged += (callMediaChangedEvent) =>
                    {
                        Console.WriteLine($"event:{callMediaChangedEvent.GetType().Name}");
                        if (callMediaChangedEvent is ReceivingAudioEvent)
                        {
                            mediaEvents.Add(callMediaChangedEvent);
                            callData.listIsReceivingAudio.Add(currentCall.IsReceivingAudio);
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });

            MessageHelper.RunDispatcherLoop();

            Assert.AreEqual(1, mediaEvents.Count);
            var mediaevent = mediaEvents[0] as ReceivingAudioEvent;
            Assert.IsNotNull(mediaevent);
            Assert.IsFalse(mediaevent.IsReceiving);
            Assert.IsTrue(callData.listIsReceivingAudio.Count > 0);
            Assert.IsFalse(callData.listIsReceivingAudio[0]);
        }

        [TestMethod()]
        public void OutgoingMediaChangedReceivingAudioEventUnMuteTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. caller: mute remote audio
            //4. caller: unmute remote audio
            //5. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndHangupAfter30Seconds(testFixtureApp);

            currentCall = null;
            List<MediaChangedEvent> mediaEvents = new List<MediaChangedEvent>();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnConnected += (call) =>
                    {
                        Console.WriteLine("onConnected");
                        Console.WriteLine("local mute remote audio");
                        currentCall.IsReceivingAudio = false;
                    };
                    currentCall.OnMediaChanged += (callMediaChangedEvent) =>
                    {
                        Console.WriteLine($"event:{callMediaChangedEvent.GetType().Name}");
                        if (callMediaChangedEvent is ReceivingAudioEvent)
                        {
                            mediaEvents.Add(callMediaChangedEvent);
                            callData.listIsReceivingAudio.Add(currentCall.IsReceivingAudio);

                            if (((ReceivingAudioEvent)callMediaChangedEvent).IsReceiving == false)
                            {
                                Console.WriteLine("local unmute remote audio");
                                currentCall.IsReceivingAudio = true;
                            }
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });

            MessageHelper.RunDispatcherLoop();

            Assert.AreEqual(2, mediaEvents.Count);
            var mediaevent = mediaEvents[0] as ReceivingAudioEvent;
            Assert.IsNotNull(mediaevent);
            Assert.IsFalse(mediaevent.IsReceiving);
            mediaevent = mediaEvents[1] as ReceivingAudioEvent;
            Assert.IsNotNull(mediaevent);
            Assert.IsTrue(mediaevent.IsReceiving);
            Assert.AreEqual(2, callData.listIsReceivingAudio.Count);
            Assert.IsFalse(callData.listIsReceivingAudio[0]);
            Assert.IsTrue(callData.listIsReceivingAudio[1]);
        }

        
        [TestMethod()]
        public void OutgoingMediaChangedCameraSwitchedEventTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. caller: switch camera
            //4. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndHangupAfter30Seconds(testFixtureApp);

            var cameras = phone.GetAVIODevices(AVIODeviceType.Camera);
            if (cameras.Count < 3)
            {
                //there is a default camera item in list, so default the list count is 2.
                Console.WriteLine("need at least two cameras.");
                //Assert.Fail();
                return;
            }
            phone.SelectAVIODevice(cameras[1]);
            var switchtoCamera = cameras[2];

            currentCall = null;
            List<MediaChangedEvent> mediaEvents = new List<MediaChangedEvent>();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnConnected += (call) =>
                    {
                        Console.WriteLine("onConnected");
                        Console.WriteLine("switch camera");
                        phone.SelectAVIODevice(switchtoCamera);
                        
                    };
                    currentCall.OnMediaChanged += (callMediaChangedEvent) =>
                    {
                        Console.WriteLine($"event:{callMediaChangedEvent.GetType().Name}");
                        if (callMediaChangedEvent is CameraSwitchedEvent)
                        {
                            mediaEvents.Add(callMediaChangedEvent);
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });

            MessageHelper.RunDispatcherLoop();
            phone.SelectAVIODevice(cameras[0]);

            Assert.AreEqual(1, mediaEvents.Count);
            var mediaevent = mediaEvents[0] as CameraSwitchedEvent;
            Assert.IsNotNull(mediaevent);
            Assert.AreEqual(switchtoCamera.Id, mediaevent.Camera.Id);
            Assert.AreEqual(switchtoCamera.Type, mediaevent.Camera.Type);
            Assert.AreEqual(switchtoCamera.Name, mediaevent.Camera.Name);
        }

        [TestMethod()]
        public void OutgoingMediaChangedSpearkerSwitchedEventTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. caller: switch speaker
            //4. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndHangupAfter30Seconds(testFixtureApp);

            var speakers = phone.GetAVIODevices(AVIODeviceType.Speaker);
            if (speakers.Count < 3)
            {
                //there is a default speaker item in list, so default the list count is 2.
                Console.WriteLine("need at least two speakers.");
                //Assert.Fail();
                return;
            }
            phone.SelectAVIODevice(speakers[1]);
            var switchtoSpeaker = speakers[2];

            currentCall = null;
            List<MediaChangedEvent> mediaEvents = new List<MediaChangedEvent>();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnConnected += (call) =>
                    {
                        Console.WriteLine("onConnected");
                        Console.WriteLine("switch speaker");
                        phone.SelectAVIODevice(switchtoSpeaker);

                    };
                    currentCall.OnMediaChanged += (callMediaChangedEvent) =>
                    {
                        Console.WriteLine($"event:{callMediaChangedEvent.GetType().Name}");
                        if (callMediaChangedEvent is SpeakerSwitchedEvent)
                        {
                            mediaEvents.Add(callMediaChangedEvent);
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });

            MessageHelper.RunDispatcherLoop();

            phone.SelectAVIODevice(speakers[0]);

            Assert.AreEqual(1, mediaEvents.Count);
            var mediaevent = mediaEvents[0] as SpeakerSwitchedEvent;
            Assert.IsNotNull(mediaevent);
            Assert.AreEqual(switchtoSpeaker.Id, mediaevent.Speaker.Id);
            Assert.AreEqual(switchtoSpeaker.Type, mediaevent.Speaker.Type);
            Assert.AreEqual(switchtoSpeaker.Name, mediaevent.Speaker.Name);
        }


        
        [TestMethod()]
        public void OutgoingMediaChangedRemoteSendingShareEventStartTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. callee: start share
            //4. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndStartShareAndHangupAfter30s(testFixtureApp);

            currentCall = null;
            List<MediaChangedEvent> mediaEvents = new List<MediaChangedEvent>();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnMediaChanged += (callMediaChangedEvent) =>
                    {
                        Console.WriteLine($"event:{callMediaChangedEvent.GetType().Name}");
                        if (callMediaChangedEvent is RemoteSendingShareEvent)
                        {
                            mediaEvents.Add(callMediaChangedEvent);
                            callData.listIsRemoteSendingShare.Add(currentCall.IsRemoteSendingShare);
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });


            MessageHelper.RunDispatcherLoop();

            Assert.IsTrue(mediaEvents.Count > 0);
            var mediaevent = mediaEvents[0] as RemoteSendingShareEvent;
            Assert.IsNotNull(mediaevent);
            Assert.IsTrue(mediaevent.IsSending);
            Assert.IsTrue(callData.listIsRemoteSendingShare.Count > 0);
            Assert.IsTrue(callData.listIsRemoteSendingShare[0]);
        }

        [TestMethod()]
        public void OutgoingMediaChangedRemoteSendingShareEventStopTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. callee: start share
            //4. callee: stop share
            //4. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndStartShare15sAndHangupAfter30s(testFixtureApp);

            currentCall = null;
            List<MediaChangedEvent> mediaEvents = new List<MediaChangedEvent>();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnMediaChanged += (callMediaChangedEvent) =>
                    {
                        Console.WriteLine($"event:{callMediaChangedEvent.GetType().Name}");
                        if (callMediaChangedEvent is RemoteSendingShareEvent)
                        {
                            mediaEvents.Add(callMediaChangedEvent);
                            callData.listIsRemoteSendingShare.Add(currentCall.IsRemoteSendingShare);
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });


            MessageHelper.RunDispatcherLoop();

            Assert.AreEqual(2, mediaEvents.Count);
            var mediaevent = mediaEvents[0] as RemoteSendingShareEvent;
            Assert.IsNotNull(mediaevent);
            Assert.IsTrue(mediaevent.IsSending);
            mediaevent = mediaEvents[1] as RemoteSendingShareEvent;
            Assert.IsNotNull(mediaevent);
            Assert.IsFalse(mediaevent.IsSending);
            Assert.AreEqual(2, callData.listIsRemoteSendingShare.Count);
            Assert.IsTrue(callData.listIsRemoteSendingShare[0]);
            Assert.IsFalse(callData.listIsRemoteSendingShare[1]);
        }

        [TestMethod()]
        public void OutgoingMediaChangedReceivingShareEventMuteTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. callee: start share
            //4. caller: mute remote share
            //5. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndStartShareAndHangupAfter30s(testFixtureApp);

            currentCall = null;
            List<MediaChangedEvent> mediaEvents = new List<MediaChangedEvent>();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnMediaChanged += (callMediaChangedEvent) =>
                    {
                        Console.WriteLine($"event:{callMediaChangedEvent.GetType().Name}");
                        if (callMediaChangedEvent is ReceivingShareEvent)
                        {
                            mediaEvents.Add(callMediaChangedEvent);
                            callData.listIsReceivingShare.Add(currentCall.IsReceivingShare);
                        }
                        if (callMediaChangedEvent is RemoteSendingShareEvent
                            && ((RemoteSendingShareEvent)callMediaChangedEvent).IsSending == true)
                        {
                            Console.WriteLine("mute remote share");
                            currentCall.IsReceivingShare = false;
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });


            MessageHelper.RunDispatcherLoop();

            Assert.AreEqual(1, mediaEvents.Count);
            var mediaevent = mediaEvents[0] as ReceivingShareEvent;
            Assert.IsNotNull(mediaevent);
            Assert.IsFalse(mediaevent.IsReceiving);
            Assert.IsTrue(callData.listIsReceivingShare.Count > 0);
            Assert.IsFalse(callData.listIsReceivingShare[0]);
        }

        [TestMethod()]
        public void OutgoingMediaChangedReceivingShareEventUnMuteTest()
        {
            //call scene：
            //1. caller: callout
            //2. callee: answer
            //3. callee: start share
            //4. caller: mute remote share
            //5. caller: unmute remote share
            //6. callee: hangup
            MessageHelper.SetTestMode_CalleeAutoAnswerAndStartShareAndHangupAfter30s(testFixtureApp);

            currentCall = null;
            List<MediaChangedEvent> mediaEvents = new List<MediaChangedEvent>();

            phone.Dial(calleeAddress, MediaOption.AudioVideoShare(), r =>
            {
                if (r.IsSuccess)
                {
                    currentCall = r.Data;
                    currentCall.OnDisconnected += (call) =>
                    {
                        Console.WriteLine("onDisconnected");
                        MessageHelper.BreakLoop();
                    };
                    currentCall.OnMediaChanged += (callMediaChangedEvent) =>
                    {
                        Console.WriteLine($"event:{callMediaChangedEvent.GetType().Name}");
                        if (callMediaChangedEvent is ReceivingShareEvent)
                        {
                            mediaEvents.Add(callMediaChangedEvent);
                            callData.listIsReceivingShare.Add(currentCall.IsReceivingShare);

                            if (((ReceivingShareEvent)callMediaChangedEvent).IsReceiving == false)
                            {
                                Console.WriteLine("unmute remote share");
                                currentCall.IsReceivingShare = true;
                            }

                        }
                        if (callMediaChangedEvent is RemoteSendingShareEvent
                            && ((RemoteSendingShareEvent)callMediaChangedEvent).IsSending == true)
                        {
                            Console.WriteLine("mute remote share");
                            currentCall.IsReceivingShare = false;
                        }
                    };
                }
                else
                {
                    Console.WriteLine($"dial fail: {r.Error.ErrorCode}:{r.Error.Reason}");
                    currentCall = r.Data;
                    MessageHelper.BreakLoop();
                }
            });


            MessageHelper.RunDispatcherLoop();

            Assert.AreEqual(2, mediaEvents.Count);
            var mediaevent = mediaEvents[0] as ReceivingShareEvent;
            Assert.IsNotNull(mediaevent);
            Assert.IsFalse(mediaevent.IsReceiving);
            mediaevent = mediaEvents[1] as ReceivingShareEvent;
            Assert.IsNotNull(mediaevent);
            Assert.IsTrue(mediaevent.IsReceiving);
            Assert.AreEqual(2, callData.listIsReceivingShare.Count);
            Assert.IsFalse(callData.listIsReceivingShare[0]);
            Assert.IsTrue(callData.listIsReceivingShare[1]);
        }

        [TestMethod()]
        public void IncomingCallStateEventTest()
        {
            phone.OnIncoming += Phone_onIncomingCallStateEventTest;

            //call scene：
            //1. remote: callout
            //2. local: answer
            //3. local: hangup
            MessageHelper.SetTestMode_RemoteDialout(testFixtureApp, self.Emails[0]);
            MessageHelper.RunDispatcherLoop();

            phone.OnIncoming -= Phone_onIncomingCallStateEventTest;
            Assert.IsTrue(callData.connectSignal);
            Assert.IsTrue(callData.disconnectSignal);
            Assert.IsTrue(callData.ReleaseReason is LocalLeft);
        }

        private void Phone_onIncomingCallStateEventTest(Call obj)
        {
            currentCall = obj;
            currentCall.Acknowledge(r =>
            { });

            currentCall.OnConnected += (call) =>
            {
                Console.WriteLine("CurrentCall_onConnected");
                callData.connectSignal = true;
                HangupCall(currentCall);
            };

            currentCall.OnDisconnected += (releaseReason) =>
            {
                Console.WriteLine("CurrentCall_onDisconnected");
                callData.disconnectSignal = true;
                callData.ReleaseReason = releaseReason;
                MessageHelper.BreakLoop();
            };

            currentCall.Answer(MediaOption.AudioVideoShare(), r =>
            {
                if (!r.IsSuccess)
                {
                    Console.WriteLine($"Phone_onIncomingCallStateEventTest: answer fail. {r.Error?.ErrorCode} {r.Error?.Reason}");
                }
            });
        }

        [TestMethod()]
        public void IncomingCallRejectTest()
        {
            phone.OnIncoming += Phone_onIncomingCallRejectTest;

            //call scene：
            //1. remote: callout
            //2. local: reject
            MessageHelper.SetTestMode_RemoteDialout(testFixtureApp, self.Emails[0]);
            MessageHelper.RunDispatcherLoop();

            phone.OnIncoming -= Phone_onIncomingCallRejectTest;
            Assert.IsNotNull(currentCall);
            Assert.IsTrue(callData.ReleaseReason is LocalDecline);
        }

        private void Phone_onIncomingCallRejectTest(Call obj)
        {
            currentCall = obj;
            currentCall.OnDisconnected += (r) =>
            {
                Console.WriteLine("CurrentCall_onDisconnected");
                callData.ReleaseReason = r;
                MessageHelper.BreakLoop();
            };
            TimerHelper.StartTimer(1000, (o, e) =>
            {
                currentCall.Reject(r =>
                {
                    if (!r.IsSuccess)
                    {
                        Console.WriteLine($"Phone_onIncomingCallRejectTest: reject fail. {r.Error?.ErrorCode} {r.Error?.Reason}");
                    }
                });
            });

        }

        [TestMethod()]
        public void DialWithHydraRoomIdAndHangUpTest()
        {
            currentCall = null;
            bool result = DialCall(myRoom.Id, MediaOption.AudioVideoShare(), ref currentCall);
            Assert.IsTrue(result);
            if (currentCall != null)
            {
                HangupCall(currentCall);
                MessageHelper.RunDispatcherLoop();
            }

            Assert.IsNotNull(currentCall);
            Assert.IsTrue(currentCall.Status >= CallStatus.Initiated);
            Assert.AreEqual(Call.CallDirection.Outgoing, currentCall.Direction);
        }

        [TestMethod()]
        public void RequestVideoCodecActivationTest()
        {
            var completion = new ManualResetEvent(false);
            phone.ActivateVideoCodecLicense(false);
            phone.DisableVideoCodecActivation(false);

            phone.OnRequestVideoCodecActivation += () =>
            {
                Assert.IsNotNull(phone.VideoCodecLicense);
                Assert.AreEqual("http://www.openh264.org/BINARY_LICENSE.txt", phone.VideoCodecLicenseURL);
                phone.ActivateVideoCodecLicense(true);
                phone.ActivateVideoCodecLicense(false);
                completion.Set();
            };

            phone.RequestVideoCodecActivation();

            if (false == completion.WaitOne(30000))
            {
                phone.DisableVideoCodecActivation(true);
                Assert.Fail();
            }
            phone.DisableVideoCodecActivation(true);
        }

        [TestMethod()]
        public void StartPreviewTest()
        {
            phone.StartPreview(IntPtr.Zero);
        }

        [TestMethod()]
        public void StopPreviewTest()
        {
            phone.StopPreview(IntPtr.Zero);
        }

        [TestMethod()]
        public void GetCamerasTest()
        {
            var devices = phone.GetAVIODevices(AVIODeviceType.Camera);
            Assert.IsNotNull(devices);
            Assert.IsTrue(devices.Count > 0);
            Assert.AreEqual(AVIODeviceType.Camera, devices[0].Type);
            Assert.IsNotNull(devices[0].Id);
            Assert.IsNotNull(devices[0].Name);
        }

        [TestMethod()]
        public void GetSpeakersTest()
        {
            var devices = phone.GetAVIODevices(AVIODeviceType.Speaker);
            Assert.IsNotNull(devices);
            Assert.IsTrue(devices.Count > 0);
            Assert.AreEqual(AVIODeviceType.Speaker, devices[0].Type);
            Assert.IsNotNull(devices[0].Id);
            Assert.IsNotNull(devices[0].Name);
        }

        [TestMethod()]
        public void GetMicrophonesTest()
        {
            var devices = phone.GetAVIODevices(AVIODeviceType.Microphone);
            Assert.IsNotNull(devices);
            Assert.IsTrue(devices.Count > 0);
            Assert.AreEqual(AVIODeviceType.Microphone, devices[0].Type);
            Assert.IsNotNull(devices[0].Id);
            Assert.IsNotNull(devices[0].Name);
        }

        [TestMethod()]
        public void GetRingersTest()
        {
            var devices = phone.GetAVIODevices(AVIODeviceType.Ringer);
            Assert.IsNotNull(devices);
            Assert.IsTrue(devices.Count > 0);
            Assert.AreEqual(AVIODeviceType.Ringer, devices[0].Type);
            Assert.IsNotNull(devices[0].Id);
            Assert.IsNotNull(devices[0].Name);
        }

        [TestMethod()]
        public void SelectCameraTest()
        {
            var devices = phone.GetAVIODevices(AVIODeviceType.Camera);
            Assert.IsNotNull(devices);
            if (devices.Count == 0)
            {
                Console.WriteLine("no camera");
                return;
            }
            Assert.IsTrue(devices.Count > 0);
            Assert.AreEqual(AVIODeviceType.Camera, devices[0].Type);
            Assert.IsNotNull(devices[0].Id);
            Assert.IsNotNull(devices[0].Name);

            Assert.IsTrue(phone.SelectAVIODevice(devices[0]));
        }

        [TestMethod()]
        public void SelectSpeakerTest()
        {
            var devices = phone.GetAVIODevices(AVIODeviceType.Speaker);
            Assert.IsNotNull(devices);
            if (devices.Count == 0)
            {
                Console.WriteLine("no speaker");
                return;
            }
            Assert.IsTrue(devices.Count > 0);
            Assert.AreEqual(AVIODeviceType.Speaker, devices[0].Type);
            Assert.IsNotNull(devices[0].Id);
            Assert.IsNotNull(devices[0].Name);

            Assert.IsTrue(phone.SelectAVIODevice(devices[0]));
        }

        private static bool RegisterPhone()
        {
            var completion = new ManualResetEvent(false);
            var response = new SparkApiEventArgs();

            spark.Phone.Register(rsp =>
            {
                response = rsp;
                completion.Set();
            });
            if (false == completion.WaitOne(120000))
            {
                Console.WriteLine("registerPhone out of time.");
                return false;
            }

            if (response.IsSuccess == true)
            {
                Console.WriteLine("registerPhone success.");
                return true;
            }

            Console.WriteLine($"registerPhone fail for {response.Error.ErrorCode.ToString()}");
            return false;
        }

        private bool DeregisterPhone()
        {
            var completion = new ManualResetEvent(false);
            var response = new SparkApiEventArgs();

            spark.Phone.Deregister(rsp =>
            {
                response = rsp;
                completion.Set();
            });
            if (false == completion.WaitOne(30000))
            {
                Console.WriteLine("deregister timeout");
                return false;
            }

            if (response.IsSuccess == true)
            {
                return true;
            }

            return false;
        }

        private bool DialCall(string address, MediaOption mediaOption, ref Call call)
        {
            var completion = new ManualResetEvent(false);
            var response = new SparkApiEventArgs<Call>();

            phone.Dial(address, mediaOption, rsp =>
            {
                response = rsp;
                completion.Set();
            });

            if (false == completion.WaitOne(30000))
            {
                Console.WriteLine("dialCall out of time");
                return false;
            }

            if (response.IsSuccess)
            {
                call = response.Data;
                call.OnDisconnected += (r) =>
                {
                    Console.WriteLine("onDisconnected");
                    callData.ReleaseReason = r;
                    MessageHelper.BreakLoop();
                };
                return true;
            }
            else
            {
                Console.WriteLine($"dial fail: {response.Error.ErrorCode}:{response.Error.Reason}");
                call = response.Data;
                return false;
            }
        }

        private void HangupCall(Call call, int after = 5000)
        {
            if (call != null)
            {
                TimerHelper.StartTimer(after, (o, e) =>
                {
                    call?.Hangup(completedHandler =>
                    { });
                });
            }
        }

        private static Room CreateRoom(string title)
        {
            var completion = new ManualResetEvent(false);
            var response = new SparkApiEventArgs<Room>();
            spark.Rooms.Create(title, null, rsp =>
            {
                response = rsp;
                completion.Set();
            });

            if (false == completion.WaitOne(30000))
            {
                return null;
            }

            if (response.IsSuccess == true)
            {
                return response.Data;
            }

            return null;
        }

        private static bool DeleteRoom(string roomId)
        {
            var completion = new ManualResetEvent(false);
            var response = new SparkApiEventArgs();
            spark.Rooms.Delete(roomId, rsp =>
            {
                response = rsp;
                completion.Set();
            });

            if (false == completion.WaitOne(30000))
            {
                return false;
            }

            if (response.IsSuccess == true)
            {
                return true;
            }

            return false;
        }

        private Membership CreateMembership(string roomId, string email, string personId, bool isModerator)
        {
            var completion = new ManualResetEvent(false);
            var response = new SparkApiEventArgs<Membership>();
            if (email != null)
            {
                spark.Memberships.CreateByPersonEmail(roomId, email, isModerator, rsp =>
                {
                    response = rsp;
                    completion.Set();
                });
            }
            else
            {
                spark.Memberships.CreateByPersonId(roomId, personId, isModerator, rsp =>
                {
                    response = rsp;
                    completion.Set();
                });
            }


            if (false == completion.WaitOne(30000))
            {
                return null;
            }

            if (response.IsSuccess == true)
            {
                return response.Data;
            }

            return null;
        }

        private List<Membership> ListMembership(string roomId, int? max = null)
        {
            var completion = new ManualResetEvent(false);
            var response = new SparkApiEventArgs<List<Membership>>();
            spark.Memberships.List(roomId, max, rsp =>
            {
                response = rsp;
                completion.Set();
            });

            if (false == completion.WaitOne(30000))
            {
                return null;
            }

            if (response.IsSuccess == true)
            {
                return response.Data;
            }

            return null;
        }

        private static Person GetMe()
        {
            var completion = new ManualResetEvent(false);
            var response = new SparkApiEventArgs<Person>();
            spark.People.GetMe(rsp =>
            {
                response = rsp;
                completion.Set();
            });

            if (false == completion.WaitOne(30000))
            {
                return null;
            }

            if (response.IsSuccess)
            {
                return response.Data;
            }
            return null;
        }

    }

    public class CallData
    {
        public bool ringSignal;
        public bool connectSignal;
        public bool disconnectSignal;

        public List<bool> listIsRemoteSendingVideo;
        public List<bool> listIsRemoteSendingAudio;
        public List<bool> listIsRemoteSendingShare;
        public List<bool> listIsSendingVideo;
        public List<bool> listIsSendingAudio;
        public List<bool> listIsReceivingVideo;
        public List<bool> listIsReceivingAudio;
        public List<bool> listIsReceivingShare;
        public List<bool> listIsSendingDTMFEnabled;

        public CallStatus Status { get; set; }
        public Call.CallDirection Direction { get; set; }


        public CallDisconnectedEvent ReleaseReason { get; set; }

        public List<CallMembershipChangedEvent> listCallMembershipChangedEvent;
        public List<MediaChangedEvent> listMediaChangedEvent;

        public CallData()
        {
            listCallMembershipChangedEvent = new List<CallMembershipChangedEvent>();
            listMediaChangedEvent = new List<MediaChangedEvent>();
            listIsRemoteSendingVideo = new List<bool>();
            listIsRemoteSendingAudio = new List<bool>();
            listIsRemoteSendingShare = new List<bool>();
            listIsSendingVideo = new List<bool>();
            listIsSendingAudio = new List<bool>();
            listIsReceivingVideo = new List<bool>();
            listIsReceivingAudio = new List<bool>();
            listIsReceivingShare = new List<bool>();
            listIsSendingDTMFEnabled = new List<bool>();
        }
    }

    class TimerHelper
    {
        public static System.Timers.Timer StartTimer(int interval, System.Timers.ElapsedEventHandler timeOutCallback)
        {
            System.Timers.Timer t = new System.Timers.Timer(interval);
            t.Elapsed += timeOutCallback;
            t.AutoReset = false;
            t.Enabled = true;

            return t;
        }
    }

    public class MessageHelper
    {
        private static SparkSDKTests.ServiceReference.TestFixtureServiceClient proxy;

        public static void Init()
        {
            proxy = new SparkSDKTests.ServiceReference.TestFixtureServiceClient();
            proxy.Open();
        }


        static bool breakLoopSignal = false;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PeekMessage(
           ref MSG lpMsg,
           Int32 hwnd,
           Int32 wMsgFilterMin,
           Int32 wMsgFilterMax,
           PeekMessageOption wRemoveMsg);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool TranslateMessage(ref MSG lpMsg);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern Int32 DispatchMessage(ref MSG lpMsg);

        private enum PeekMessageOption
        {
            PM_NOREMOVE = 0,
            PM_REMOVE
        }
        public const int WM_QUIT = 0x0012;
        public const int WM_COPYDATA = 0x004A;


        [StructLayout(LayoutKind.Sequential)]
        public struct CopyDataStruct
        {
            public IntPtr dwData;
            public int cbData;

            [MarshalAs(UnmanagedType.LPStr)]
            public string lpData;
        }


        public static void RunDispatcherLoop()
        {
            MSG msg = new MSG();
            // max loop time 2 minute
            var t = TimerHelper.StartTimer(120000, (o,e)=>
            {
                breakLoopSignal = true;
            });

            while (true)
            {
                if (PeekMessage(ref msg, 0, 0, 0, PeekMessageOption.PM_REMOVE))
                {
                    if (msg.message == WM_QUIT)
                    {
                        Console.WriteLine("break loop");
                        break;
                    }

                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }

                if (breakLoopSignal)
                {
                    breakLoopSignal = false;
                    t.Stop();
                    t.Close();
                    break;
                }
            }
        }

        public static void BreakLoop()
        {
            breakLoopSignal = true;
        }




        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage
        (
            IntPtr hWnd,                   //目标窗体句柄
            int Msg,                       //WM_COPYDATA
            int wParam,                    //自定义数值
            ref CopyDataStruct lParam     //结构体
        );

        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        public static void SendMessage(string windowName, string strMsg)
        {
            StackTrace st = new StackTrace(true);
            StackFrame sf = st.GetFrame(2);

            MessageHelper.proxy.SendCommandMsg(sf.GetMethod().Name + ":" + strMsg);
            Thread.Sleep(500);

            //if (strMsg == null) return;

            //IntPtr hwnd = FindWindow(null, windowName);

            //if (hwnd != IntPtr.Zero)
            //{
            //    CopyDataStruct cds;

            //    cds.dwData = IntPtr.Zero;
            //    cds.lpData = strMsg;

            //    cds.cbData = System.Text.Encoding.Default.GetBytes(strMsg).Length + 1;

            //    int fromWindowHandler = 0;
            //    SendMessage(hwnd, WM_COPYDATA, fromWindowHandler, ref cds);
            //}
        }

        public static void SendMessageByProcess(string processName, string strMsg)
        {
            if (strMsg == null) return;
            var process = Process.GetProcessesByName(processName);
            if (process.FirstOrDefault() == null) return;
            var hwnd = process.FirstOrDefault().MainWindowHandle;
            if (hwnd == IntPtr.Zero) return;

            if (hwnd != IntPtr.Zero)
            {
                CopyDataStruct cds;

                cds.dwData = IntPtr.Zero;
                cds.lpData = strMsg;

                cds.cbData = System.Text.Encoding.Default.GetBytes(strMsg).Length + 1;

                int fromWindowHandler = 0;
                SendMessage(hwnd, WM_COPYDATA, fromWindowHandler, ref cds);

            }
        }

        public static void SetTestMode_CalleeAutoAnswerAndHangupAfter30Seconds(string windowName)
        {
            MessageHelper.SendMessage(windowName, "Enable");
            MessageHelper.SendMessage(windowName, "AutoAnswer");
            MessageHelper.SendMessage(windowName, "ConversationTimer:30000");
        }

        public static void SetTestMode_CalleeAutoDecline(string windowName)
        {
            MessageHelper.SendMessage(windowName, "Enable");
            MessageHelper.SendMessage(windowName, "AutoDecline");
        }

        public static void SetTestMode_CalleeAutoAnswerAndMuteVideoAndHangupAfter30Seconds(string windowName)
        {
            MessageHelper.SendMessage(windowName, "Enable");
            MessageHelper.SendMessage(windowName, "AutoAnswer");
            MessageHelper.SendMessage(windowName, "MuteVideo");
            MessageHelper.SendMessage(windowName, "ConversationTimer:30000");
        }

        public static void SetTestMode_CalleeAutoAnswerAndMuteVideoAndUnMuteVideoAndHangupAfter30Seconds(string windowName)
        {
            MessageHelper.SendMessage(windowName, "Enable");
            MessageHelper.SendMessage(windowName, "AutoAnswer");
            MessageHelper.SendMessage(windowName, "MuteVideo:5000");
            MessageHelper.SendMessage(windowName, "ConversationTimer:30000");
        }

        public static void SetTestMode_CalleeAutoAnswerAndMuteAudioAndHangupAfter30Seconds(string windowName)
        {
            MessageHelper.SendMessage(windowName, "Enable");
            MessageHelper.SendMessage(windowName, "AutoAnswer");
            MessageHelper.SendMessage(windowName, "MuteAudio");
            MessageHelper.SendMessage(windowName, "ConversationTimer:30000");
        }

        public static void SetTestMode_CalleeAutoAnswerAndMuteAudioAndUnMuteAudioAndHangupAfter30Seconds(string windowName)
        {
            MessageHelper.SendMessage(windowName, "Enable");
            MessageHelper.SendMessage(windowName, "AutoAnswer");
            MessageHelper.SendMessage(windowName, "MuteAudio:5000");
            MessageHelper.SendMessage(windowName, "ConversationTimer:30000");
        }

        public static void SetTestMode_CalleeAutoAnswerAndStartShareAndHangupAfter30s(string windowName)
        {
            MessageHelper.SendMessage(windowName, "Enable");
            MessageHelper.SendMessage(windowName, "AutoAnswer");
            MessageHelper.SendMessage(windowName, "StartShare");
            MessageHelper.SendMessage(windowName, "ConversationTimer:30000");
        }

        public static void SetTestMode_CalleeAutoAnswerAndStartShare15sAndHangupAfter30s(string windowName)
        {
            MessageHelper.SendMessage(windowName, "Enable");
            MessageHelper.SendMessage(windowName, "AutoAnswer");
            MessageHelper.SendMessage(windowName, "StartShare:10000");
            MessageHelper.SendMessage(windowName, "ConversationTimer:30000");
        }

        public static void SetTestMode_RemoteDialout(string windowName, string address)
        {
            MessageHelper.SendMessage(windowName, "Enable");
            MessageHelper.SendMessage(windowName, "Dial:"+ address);
        }

        public static void CloseTestFixtureApp(string windowName)
        {
            MessageHelper.SendMessage(windowName, "CloseApp");
        }
    }
}