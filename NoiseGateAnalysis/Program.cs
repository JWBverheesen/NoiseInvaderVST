﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioLib;
using AudioLib.Modules;
using AudioLib.TF;
using LowProfile.Visuals;
using OxyPlot;
using OxyPlot.Axes;

namespace NoiseGate
{
	class Program
	{
		public static double ReductionDb = -40;
		public static double ThresholdDb = -20;
		public static double UpperSlope = 2;
		public static double ReleaseMs = 20;

		private static double fs = 48000.0;
		
		public void Run()
		{
			/*var pm = new PlotModel();
			var xs = Enumerable.Range(0, 1000).Select(x => x / 1000.0).Select(x => -100 + x * 100).ToArray();
			var ys1 = xs.Select(x => Compress(x, ThresholdDb, UpperSlope, 3, true)).ToArray();
			var ys2 = xs.Select(x => Compress(x, ThresholdDb + 6, LowerSlope, 3, true)).ToArray();

			pm.AddLine(xs, ys1);
			pm.AddLine(xs, ys2);
			pm.Show();
			*/
			var pm = new PlotModel();
			pm.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Key = "L" });
			pm.Axes.Add(new LinearAxis { Position = AxisPosition.Right, Key = "R" });
			
			var follower = new EnvelopeFollower(48000, 20);
			var expander = new Expander();
			var slewLimiter = new SlewLimiter(48000);
			follower.SetRelease(ReleaseMs);
			slewLimiter.UpdateDb60(2.0, ReleaseMs);
			expander.Update(ThresholdDb, ReductionDb, UpperSlope);
			var signalValues = new List<double>();
			var followerValues = new List<double>();
			var gainValues = new List<double>();

			var rand = new Random();
			Func<double> random = () => 2 * rand.NextDouble() - 1;
			double decay = 1.0;

			for (int i = 0; i < 20000; i++)
			{
				var x = (Math.Sin(i / fs * 2 * Math.PI * 300) + random() * 0.4) *decay;
				decay *= 0.9998;
				if (i < 1000)
					x = random() * 0.001;
				else if (i > 12000)
				{
					x = random() * 0.001;
				}
				
				signalValues.Add(x);
				follower.ProcessEnvelope(x);
				var env = follower.GetOutput();
				followerValues.Add(env);

				expander.Expand(Utils.Gain2Db(env));
				var gainDb = expander.GetOutput();
				gainDb = slewLimiter.Process(gainDb);
				var gain = Utils.Db2Gain(gainDb);
                gainValues.Add(gain);
			}

			var signalLine = pm.AddLine(signalValues);
			signalLine.Title = "Signal";
			signalLine.YAxisKey = "R";
			//pm.AddLine(followerValues.Select(Utils.Gain2Db)).Title = "followerValues";
			pm.AddLine(gainValues.Select(Utils.Gain2Db)).Title = "gainValues";

			pm.Show();
		}

		[STAThread]
		static void Main(string[] args)
		{
			new Program().Run();
		}

	}
}
