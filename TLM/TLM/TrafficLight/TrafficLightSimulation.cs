using System;
using ColossalFramework;
using TrafficManager.Traffic;

namespace TrafficManager.TrafficLight {
    public class TrafficLightSimulation {
        public readonly ushort nodeId;

        public bool ManualTrafficLights;
        public bool TimedTrafficLights;
        public bool TimedTrafficLightsActive;

        public bool FlagManualTrafficLights {
            get { return ManualTrafficLights; }
            set {
                ManualTrafficLights = value;

                if (value == false) {
                    var node = getNode();

                    for (var s = 0; s < 8; s++) {
                        var segment = node.GetSegment(s);

                        if (segment == 0) continue;
                        if (TrafficLightsManual.IsSegmentLight(nodeId, segment)) {
                            TrafficLightsManual.RemoveSegmentLight(nodeId, segment);
                        }
                    }
                } else {
					var node = getNode();

                    for (var s = 0; s < 8; s++) {
                        var segmentId = node.GetSegment(s);

                        if (segmentId == 0)
							continue;
						TrafficLightsManual.AddLiveSegmentLight(nodeId, segmentId);
                    }
                }
            }
        }

        public bool FlagTimedTrafficLights {
            get { return TimedTrafficLights; }
            set {
                TimedTrafficLights = value;

                if (value == false) {
                    TimedTrafficLightsActive = false;
					TrafficLightsTimed timedLight = TrafficLightsTimed.GetTimedLight(nodeId);
					if (timedLight != null)
						timedLight.Stop();
					TrafficLightsTimed.RemoveTimedLight(nodeId);

					var node = getNode();

					for (int s = 0; s < 8; s++) {
                        var segment = node.GetSegment(s);

                        if (segment == 0) continue;
                        if (TrafficLightsManual.IsSegmentLight(nodeId, segment)) {
                            TrafficLightsManual.RemoveSegmentLight(nodeId, segment);
                        }
                    }
                } else {
                    TrafficLightsTimed.AddTimedLight(nodeId, TrafficLightTool.SelectedNodeIndexes);

					var node = getNode();

                    for (int s = 0; s < 8; s++) {
                        var segmentId = node.GetSegment(s);

						if (segmentId == 0)
							continue;
						TrafficLightsManual.AddLiveSegmentLight(nodeId, segmentId);
					}
                }
            }
        }

        public TrafficLightSimulation(ushort nodeId) {
            this.nodeId = nodeId;
		}

        public void SimulationStep() {
            //Log.Warning("step: " + NodeId);
            var currentFrameIndex = Singleton<SimulationManager>.instance.m_currentFrameIndex;

			if (TimedTrafficLightsActive) {
				var timedNode = TrafficLightsTimed.GetTimedLight(nodeId);
				if (timedNode != null)
					timedNode.CheckCurrentStep();
            }

			var node = getNode();
			for (var l = 0; l < 8; l++) {
                var segment = node.GetSegment(l);
                if (segment == 0) continue;
                if (!TrafficLightsManual.IsSegmentLight(nodeId, segment)) continue;

                var segmentLight = TrafficLightsManual.GetSegmentLight(nodeId, segment);

                segmentLight.LastChange = (currentFrameIndex >> 6) - segmentLight.LastChangeFrame;
            }
        }

		public NetNode getNode() {
			return Singleton<NetManager>.instance.m_nodes.m_buffer[nodeId];
		}

		/// <summary>
		/// Stops & destroys the traffic light simulation(s) at this node (group)
		/// </summary>
		public void Destroy() {
			var node = getNode();
			var timedNode = TrafficLightsTimed.GetTimedLight(nodeId);

			if (timedNode != null) {
				foreach (var timedNodeId in timedNode.NodeGroup) {
					var nodeSim = TrafficPriority.GetNodeSimulation(timedNodeId); // `this` is one of `nodeSim`
					TrafficLightsTimed.RemoveTimedLight(timedNodeId);
					if (nodeSim == null)
						continue;
					nodeSim.TimedTrafficLightsActive = false;
					nodeSim.TimedTrafficLights = false;
				}
			}
		}
	}
}
