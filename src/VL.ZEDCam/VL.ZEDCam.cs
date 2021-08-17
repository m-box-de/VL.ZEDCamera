using sl;
using System;
using System.Collections.Generic;
using System.Threading;


namespace sl
{
	public class ZEDCam
	{
		ZEDCamera camera;
		InitParameters initParameters;
		object grabLock = new object();

		ERROR_CODE lastInitStatus = ERROR_CODE.ERROR_CODE_LAST;
		ERROR_CODE previousInitStatus;


		private bool running;
		public bool Running
		{
			get => running;
		}

		List<ZEDMat> mats;
		int matIndex;

		private RuntimeParameters runtimeParameters;
		public RuntimeParameters RuntimeParameters
		{
			get { return this.runtimeParameters; }
			set { this.runtimeParameters = value; }
		}

        public float FOV_H
        {
            get { return this.camera.HorizontalFieldOfView; }
            //get { return this.camera.CalibrationParametersRectified.leftCam.hFOV; }
        }

        public float FOV_V
        {
            get { return this.camera.VerticalFieldOfView; }
            //get { return this.camera.CalibrationParametersRectified.leftCam.vFOV; }
        }

        public int FPS
        {
            get { return (int)this.camera.GetCameraFPS(); }
            set { this.camera.SetCameraFPS(value); }
        }

        public void setFPS (fps x)
        {
            int t = 30;
            switch(x)
            {
                case fps._15FPS: t = 15; break;
                case fps._30FPS: t = 30; break;
                case fps._60FPS: t = 60; break;
                case fps._100FPS:t = 100; break;
            }
            FPS = t;
        }

        // constructor
        public ZEDCam() : this(null, null) { }

		public ZEDCam(string svoPath) : this(null, svoPath) { }

		public ZEDCam(InitParameters parameters = null, string svoPath = null)
		{
			camera = new ZEDCamera();
            
            mats = new List<ZEDMat>();

			if (parameters == null)
			{
				parameters = new InitParameters();
				parameters.resolution = RESOLUTION.HD720;
				parameters.depthMode = DEPTH_MODE.QUALITY;
				parameters.depthStabilization = true;
				parameters.enableRightSideMeasure = true; // isStereoRig;

				parameters.coordinateUnit = UNIT.METER;
				parameters.depthMinimumDistance = 200f;
            }

			if (svoPath != null)
			{
				parameters.pathSVO = svoPath;
			}

			this.initParameters = parameters;

			// runtime parameters
			runtimeParameters = new RuntimeParameters()
			{
				sensingMode = SENSING_MODE.FILL,
				enableDepth = true
			};

			// create the camera
			camera.CreateCamera(0,true);
		}

		public ZEDCam(RESOLUTION resolution, DEPTH_MODE depthMode = DEPTH_MODE.QUALITY, bool stabilisation = true)
			: this(new InitParameters
			{
				resolution = resolution,
				depthMode = depthMode,
				depthStabilization = stabilisation,
				enableRightSideMeasure = true,

				coordinateUnit = UNIT.METER,
				depthMinimumDistance = 200f
			})
		{ }

        public ZEDMat RetrieveImage(ZEDMat mat, VIEW view)
        {
            camera.RetrieveImage(mat, view);
            return mat;
        }

        public ZEDMat RetrieveMeasure(ZEDMat mat, MEASURE measure)
        {
            camera.RetrieveMeasure(mat, measure);
            return mat;
        }

        public void InitZED_Sequential()
		{
			while (lastInitStatus != sl.ERROR_CODE.SUCCESS)
			{
				lastInitStatus = camera.Init(ref initParameters);
                if (lastInitStatus != sl.ERROR_CODE.SUCCESS)
				{
					lastInitStatus = camera.Init(ref initParameters);
					previousInitStatus = lastInitStatus;
				}
				Thread.Sleep(300);
			}
        }

		public void Start_Sequential()
		{
			if (running == true) return;
			InitZED_Sequential();
			running = true;
		}

		public void GrabOnce()
		{
			if (running)
			{
				lock (grabLock)
				{
					sl.ERROR_CODE e = camera.Grab(ref runtimeParameters);
					// reset mat index
					matIndex = 0;
				}
			}
			else
			{
				//Console.WriteLine("Not open");
			}
		}

		public void Stop()
		{
			this.running = false;
		}

		public void Close()
		{
            camera.Destroy();
		}

		public void TT()
		{
			camera.SetCameraSettings();
		}

        public enum fps
        {
            /// <summary>
            /// 2208*1242. Supported frame rate: 15 FPS.
            /// </summary>
            _15FPS,
            /// <summary>
            /// 1920*1080. Supported frame rates: 15, 30 FPS.
            /// </summary>
            _30FPS,
            /// <summary>
            /// 1280*720. Supported frame rates: 15, 30, 60 FPS.
            /// </summary>
            _60FPS,
            /// <summary>
            /// 672*376. Supported frame rates: 15, 30, 60, 100 FPS.
            /// </summary>
            _100FPS
        };

		public sl.ERROR_CODE StartDetection (ref dll_ObjectDetectionParameters od_params)
        {
			return camera.EnableObjectsDetection(ref od_params);
		}

		public void StopDetection()
        {
			camera.DisableObjectsDetection();
		}
		
		public void PouseDetection(bool status)
		{
			camera.PauseObjectsDetection(status);
		}

		public sl.ERROR_CODE RetrieveDetectionData(ref dll_ObjectDetectionRuntimeParameters od_params, ref ObjectsFrameSDK objFrame)
        {
			return camera.RetrieveObjectsDetectionData(ref od_params, ref objFrame);
		}
	}
}
