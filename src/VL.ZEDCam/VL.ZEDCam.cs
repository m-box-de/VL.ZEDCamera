using sl;
using System;
using System.Collections.Generic;
using System.Threading;
using OpenCvSharp;
using System.Runtime.InteropServices;


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

				parameters.coordinateUnit = UNIT.MILLIMETER;
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

				coordinateUnit = UNIT.MILLIMETER,
				depthMinimumDistance = 200f
			})
		{ }

		public Mat FetchImage(VIEW view)
		{
			if (mats.Count < matIndex + 1)
			{
				mats.Add(new ZEDMat());
			}
			var mat = mats[matIndex++];
			camera.RetrieveImage(mat, view);

			MatType cvType;

			switch (view)
			{
				case VIEW.LEFT:						cvType = MatType.CV_8UC4; break;
				case VIEW.RIGHT:					cvType = MatType.CV_8UC4; break;
				case VIEW.LEFT_GREY:				cvType = MatType.CV_8UC1; break;
				case VIEW.RIGHT_GREY:				cvType = MatType.CV_8UC1; break;
				case VIEW.LEFT_UNRECTIFIED:			cvType = MatType.CV_8UC4; break;
				case VIEW.RIGHT_UNRECTIFIED:		cvType = MatType.CV_8UC4; break;
				case VIEW.LEFT_UNRECTIFIED_GREY:	cvType = MatType.CV_8UC1; break;
				case VIEW.RIGHT_UNRECTIFIED_GREY:	cvType = MatType.CV_8UC1; break;
				case VIEW.SIDE_BY_SIDE:				cvType = MatType.CV_8UC4; break;
				case VIEW.DEPTH:					cvType = MatType.CV_32FC1; break;
				case VIEW.CONFIDENCE:				cvType = MatType.CV_8UC4; break;
				case VIEW.NORMALS:					cvType = MatType.CV_8UC4; break;
				case VIEW.DEPTH_RIGHT:				cvType = MatType.CV_8UC4; break;
				case VIEW.NORMALS_RIGHT:			cvType = MatType.CV_8UC4; break;
				default:							cvType = -1; break;
			}
			return new Mat(camera.ImageHeight, view == VIEW.SIDE_BY_SIDE ? camera.ImageWidth * 2 : camera.ImageWidth, cvType, mat.GetPtr());
		}

		public Mat FetchPointCloud(MEASURE measure)
		{
			if (mats.Count < matIndex + 1)
			{
				mats.Add(new ZEDMat());
			}
			var depth_zed = mats[matIndex++];
			camera.RetrieveMeasure(depth_zed, measure);

			MatType cvType;

			switch (measure)
			{
				case MEASURE.DISPARITY:			cvType = MatType.CV_32FC1; break;
				case MEASURE.DEPTH:				cvType = MatType.CV_32FC1; break;
				case MEASURE.CONFIDENCE:		cvType = MatType.CV_32FC1; break;
				case MEASURE.XYZ:				cvType = MatType.CV_32FC4; break;
				case MEASURE.XYZRGBA:			cvType = MatType.CV_32FC4; break;
				case MEASURE.XYZBGRA:			cvType = MatType.CV_32FC4; break;
				case MEASURE.XYZARGB:			cvType = MatType.CV_32FC4; break;
				case MEASURE.XYZABGR:			cvType = MatType.CV_32FC4; break;
				case MEASURE.NORMALS:			cvType = MatType.CV_32FC4; break;
				case MEASURE.DISPARITY_RIGHT:	cvType = MatType.CV_32FC1; break;
				case MEASURE.DEPTH_RIGHT:		cvType = MatType.CV_32FC1; break;
				case MEASURE.XYZ_RIGHT:			cvType = MatType.CV_32FC4; break;
				case MEASURE.XYZRGBA_RIGHT:		cvType = MatType.CV_32FC4; break;
				case MEASURE.XYZBGRA_RIGHT:		cvType = MatType.CV_32FC4; break;
				case MEASURE.XYZARGB_RIGHT:		cvType = MatType.CV_32FC4; break;
				case MEASURE.XYZABGR_RIGHT:		cvType = MatType.CV_32FC4; break;
				default:						cvType = -1; break;
			}

			return new Mat(camera.ImageHeight, camera.ImageWidth, cvType, depth_zed.GetPtr());
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

        public void SetCamFPS(int FPS, out int CurrentFPS)
        {
            float currentFPS = this.camera.GetCameraFPS();
            if ((int)currentFPS != FPS)
                this.camera.SetCameraFPS(FPS);

            CurrentFPS = (int)this.camera.GetCameraFPS();
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
	}
}
