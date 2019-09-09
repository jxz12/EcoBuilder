using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EcoBuilder.Archie
{


    public class meta : MonoBehaviour
{
	// copied from my work in Metaball.cs
	protected class metaball{
		private Vector3 coordinate;
		private float radius;
		public float X {
			get {
				return coordinate.x;
			}
			set {
				coordinate.x = value;
			}
		}
		public float Y {
			get {
				return coordinate.y;
			}
			set {
				coordinate.y = value;
			}
		}
		public float Z {
			get {
				return coordinate.z;
			}
			set {
				coordinate.z = value;
			}
		}
		public float Radius {
			get {
				return radius;
			}
			set {
				radius = value;
			}
		}

		public float evaluated_distance(Vector3 point_position) {
			return (1/((coordinate.x - point_position.x)*(coordinate.x - point_position.x)+(coordinate.y - point_position.y)*(coordinate.y - point_position.y)+(coordinate.z - point_position.z)*(coordinate.z - point_position.z))) * radius * radius;
		}

		public metaball(float initial_x, float initial_y, float initial_z, float initial_radius) {
			coordinate.x = initial_x;
			coordinate.y = initial_y;
			coordinate.z = initial_z;
			radius = initial_radius;
		}
	}
}

}