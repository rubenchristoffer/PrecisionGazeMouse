﻿using System;
using System.Drawing;
using System.Timers;
using TrackIRUnity;

namespace PrecisionGazeMouse.PrecisionPointers
{
    public class HeadRotation
    {
        public float pitch { get; set; }
        public float yaw { get; set; }
    }

    public class HeadTranslation
    {
        public int x { get; set; }
        public int y { get; set; }
    }

    /*
     * Using https://github.com/medsouz/Unity-TrackIR-Plugin-DLL
     */
    class TrackIRPrecisionPointer : PrecisionPointer
    {
        TrackIRUnity.TrackIRClient trackIRclient;
        bool started;
        Timer aTimer;
        TrackIRClient.LPTRACKIRDATA tid;
        PrecisionPointerMode mode;
        HeadRotation rot;
        HeadTranslation trans;
        int sensitivity;

        public TrackIRPrecisionPointer(PrecisionPointerMode mode, int sensitivity)
        {
            this.mode = mode;
            trackIRclient = new TrackIRUnity.TrackIRClient();  // Create an instance of the TrackerIR Client to get data from.
            this.sensitivity = sensitivity;

            if (trackIRclient != null)
            {
                string status = trackIRclient.TrackIR_Enhanced_Init();
                if (status != null)
                {
                    started = true;
                    
                    aTimer = new System.Timers.Timer(33);
                    aTimer.Elapsed += Update;
                    aTimer.AutoReset = false;
                    aTimer.Enabled = true;
                }
            }
        }

        public PrecisionPointerMode Mode
        {
            get { return mode; }
            set { mode = value; }
        }

        public int Sensitivity
        {
            get { return sensitivity; }
            set { sensitivity = value; }
        }

        public bool IsStarted()
        {
            return started;
        }

        public override String ToString()
        {
            switch (mode)
            {
                case (PrecisionPointerMode.ROTATION):
                    if (rot == null)
                        return "";
                    else
                        return String.Format("({0:0}, {1:0})", rot.yaw, rot.pitch);
                case (PrecisionPointerMode.TRANSLATION):
                    if (trans == null)
                        return "";
                    else
                        return String.Format("({0:0}, {1:0})", trans.x, trans.y);
                case (PrecisionPointerMode.BOTH):
                    if (rot == null && trans == null)
                        return "";
                    else
                        return String.Format("({0:0}, {1:0})", trans.x + rot.yaw, rot.pitch);
            }

            return "";
        }

        public Point GetNextPoint(Point warpPoint)
        {
            Rectangle screenSize = PrecisionGazeMouseForm.GetScreenSize();
            switch (mode) {
                case (PrecisionPointerMode.ROTATION):
                    rot = this.getRotation ();
                    if (rot != null) {
                        double basePitch = (warpPoint.Y - screenSize.Height / 2.0) / (screenSize.Height / 2.0) * 200.0;
                        int yOffset = (int) ((rot.pitch - basePitch) * sensitivity / 20);

                        double baseYaw = (warpPoint.X - screenSize.Width / 2.0) / (screenSize.Width / 2.0) * 600.0;
                        int xOffset = (int) ((-1 * rot.yaw - baseYaw) * sensitivity / 20);

                        warpPoint.Offset (xOffset, yOffset);

                        return warpPoint;
                    }
                    break;
                case (PrecisionPointerMode.TRANSLATION):
                    trans = this.getTranslation ();
                    if (trans != null) {
                        warpPoint.Offset ((int) (trans.x / 1.5), (int) (trans.y / 1.5));
                        return warpPoint;
                    }
                    break;
                case (PrecisionPointerMode.BOTH):
                    trans = this.getTranslation ();
                    if (trans != null) {
                        warpPoint.Offset ((int) (trans.x / 1.5), 0); // set y to zero because translating head/up down is difficult, and it throws off the rotation
                    }
                    rot = this.getRotation ();
                    if (rot != null) {
                        double basePitch = (warpPoint.Y - screenSize.Height / 2.0) / (screenSize.Height / 2.0) * 200.0;
                        int yOffset = (int) ((rot.pitch - basePitch) * sensitivity / 20);

                        double baseYaw = (warpPoint.X - screenSize.Width / 2.0) / (screenSize.Width / 2.0) * 600.0;
                        int xOffset = (int) ((-1 * rot.yaw - baseYaw) * sensitivity / 20);

                        warpPoint.Offset (xOffset, yOffset);
                    }
                    return warpPoint;
                case (PrecisionPointerMode.JOYSTICK):
                    rot = this.getRotation ();

                    float xValue = -1 * rot.yaw / 100;
                    float yValue = rot.pitch / 100;

                    warpPoint.X = (int) Math.Floor (xValue / 2f);
                    warpPoint.X = Math.Max (-10, Math.Min (10, warpPoint.X));

                    if (Math.Abs (xValue) < 5)
                        warpPoint.X = 0;

                    warpPoint.Y = (int) Math.Floor (yValue / 2f);
                    warpPoint.Y = Math.Max (-10, Math.Min (10, warpPoint.Y));

                    if (Math.Abs (yValue) < 5)
                        warpPoint.Y = 0;

                    return warpPoint;
            }

            return warpPoint;
        }

        ~TrackIRPrecisionPointer()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (trackIRclient != null && started)
            {                         // Stop tracking
                string status = trackIRclient.TrackIR_Shutdown();
                trackIRclient = null;
                started = false;
            }
        }

        // Update is called once per frame
        void Update(Object source, ElapsedEventArgs e)
        {
            if (started)
            {
                //data = trackIRclient.client_TestTrackIRData();          // Data for debugging output, can be removed if not debugging/testing
                tid = trackIRclient.client_HandleTrackIRData(); // Data for head tracking
            }
            aTimer.Start();
        }
        
        HeadRotation getRotation()
        {
            HeadRotation o = new HeadRotation();
            o.pitch = tid.fNPPitch;
            o.yaw = tid.fNPYaw;
            return o;
        }

        HeadTranslation getTranslation()
        {
            HeadTranslation o = new HeadTranslation();
            o.x = -1 * (int)tid.fNPX;
            o.y = -1 * (int)tid.fNPY;
            return o;
        }
    }
}
