using System;
using System.Threading;
using UnityEngine;

namespace Janelia
{

    public static class JetTracIdentifier
    {
        public static bool Identify(ref int ballDeviceNum, ref int headDeviceNum)
        {
            RawHidReader reader = new RawHidReader();
            byte[] bytes1 = new byte[RawHidReader.READ_SIZE_BYTES];
            byte[] bytes2 = new byte[RawHidReader.READ_SIZE_BYTES];
            long timestampMs = 0;

            bool foundBall = false;
            bool foundHead = false;

            int rawHidDeviceCount = reader.Start();

            Debug.Log("JetTracIdentifier: found " + rawHidDeviceCount + " raw HID devices");

            Thread.Sleep(500);
            for (int deviceNum = 0; deviceNum < rawHidDeviceCount; ++deviceNum)
            {
                if (!Take(reader, deviceNum, ref bytes1, ref timestampMs))
                {
                    Debug.Log("JetTracIdentifier: Take " + deviceNum + ".1 failed");
                    return false;
                }
                if (!Take(reader, deviceNum, ref bytes2, ref timestampMs))
                {
                    Debug.Log("JetTracIdentifier: Take " + deviceNum + ".2 failed");
                    return false;
                }

                JetTracParser.BallMessage ballMessage1 = new JetTracParser.BallMessage();
                if (JetTracParser.ParseBallMessage(ref ballMessage1, bytes1, timestampMs))
                {
                    JetTracParser.BallMessage ballMessage2 = new JetTracParser.BallMessage();
                    if (JetTracParser.ParseBallMessage(ref ballMessage2, bytes2, timestampMs))
                    {
                        if (HasJetTracFrequency(ballMessage1.deviceTimestampUs, ballMessage2.deviceTimestampUs))
                        {
                            ballDeviceNum = deviceNum;
                            foundBall = true;

                            Debug.Log("JetTracIdentifier.Identify found ballDeviceNum " + ballDeviceNum);
                        }
                    }
                }
                
                JetTracParser.HeadMessage headMessage1 = new JetTracParser.HeadMessage();
                if (JetTracParser.ParseHeadMessage(ref headMessage1, bytes1, timestampMs))
                {
                    JetTracParser.HeadMessage headMessage2 = new JetTracParser.HeadMessage();
                    if (JetTracParser.ParseHeadMessage(ref headMessage2, bytes2, timestampMs))
                    {
                        if (HasJetTracFrequency(headMessage1.deviceTimestampUs, headMessage2.deviceTimestampUs))
                        {
                            headDeviceNum = deviceNum;
                            foundHead = true;                   

                            Debug.Log("JetTracIdentifier.Identify found headDeviceNum " + headDeviceNum);
                        }
                    }
                }
            }

            reader.OnDisable();

            if (!foundBall || !foundHead)
            {
                // At this point, just guess.
                ballDeviceNum = rawHidDeviceCount - 1;
                headDeviceNum = ballDeviceNum - 1;

                Debug.Log("JetTracIdentifier.Identify guessing ballDeviceNum " + ballDeviceNum + " headDeviceNum " + headDeviceNum);
            }
            return true;
        }

        private static bool Take(RawHidReader reader, int deviceNum, ref Byte[] taken, ref long timestampMs)
        {
            for (int i = 0; i < 10 * JetTracTransformer.DEVICE_RATE_HZ; ++i)
            {
                if (reader.Take(deviceNum, ref taken, ref timestampMs))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasJetTracFrequency(UInt64 deviceTimestampUs1, UInt64 deviceTimestampUs2)
        {
            UInt64 targetDeltaTimestampUs = (UInt64) (1.0 / JetTracTransformer.DEVICE_RATE_HZ * 1000 * 1000);
            UInt64 minDeltaTimestampUs = targetDeltaTimestampUs - 10;
            UInt64 maxDeltaTimestampUs = targetDeltaTimestampUs + 10;
            UInt64 deltaDeviceTimestampUs = deviceTimestampUs2 - deviceTimestampUs1;
            return ((minDeltaTimestampUs < deltaDeviceTimestampUs) && (deltaDeviceTimestampUs < maxDeltaTimestampUs));
        }
    }
}