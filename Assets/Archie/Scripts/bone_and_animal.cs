using System;		
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using connect;

namespace EcoBuilder.Archie
{


    public class bone_and_animal : Organism {
	protected class bone {
		protected metaball upper_joint;
		protected metaball lower_joint;

		protected float length;
		public float Length {
			get{
				return length;
			}
			set{
				length = value;
				update_upper_joint_coordinates();
			}
		}

		protected float plane_angle; // the angle of the projection into the plane xz
		protected float height_angle; // the angle from the projection into the plane xy
		public virtual float XZ {
			get {
				return plane_angle;
			}
			set {
				plane_angle = (value % (float)(2*System.Math.PI));
				update_upper_joint_coordinates();
			}
		}
		public virtual float XY {
			get {
				return height_angle;
			}
			set {
				height_angle = (value % (float)(2*System.Math.PI));
				update_upper_joint_coordinates();
			}
		}

		public void update_upper_joint_coordinates() {
			upper_joint.Y = (lower_joint.Y + (float)System.Math.Sin(height_angle) * length) ;

			float plane_distance = (float)System.Math.Cos(height_angle) * length;
			upper_joint.X = lower_joint.X + (float)System.Math.Cos(plane_angle) * plane_distance;
			upper_joint.Z = lower_joint.Z + (float)System.Math.Sin(plane_angle) * plane_distance;
		}
		
		public bone(metaball lower, metaball upper){
			upper_joint = upper;
			lower_joint = lower;
			Vector3 lowpos = new Vector3(lower.X, lower.Y, lower.Z);
			Vector3 upppos = new Vector3(upper.X, upper.Y, upper.Z);
			length = (upppos-lowpos).magnitude;
			height_angle = 0;
			plane_angle = 0;
			update_upper_joint_coordinates();
		}
	}

	protected class leg_bone : bone {
		public override float XZ { // this is restricted to the range [-pi/2, pi/2]
			get {
				return plane_angle;
			}
			set {
				float fpi = (float)(System.Math.PI); // float value of pi
				// float input_angle = (value % (float)(System.Math.PI/2));
				float input_angle = value % (2*fpi);
				if (input_angle < 0) { // this if statement is necessary if we want to achieve the result we would have from the modulo operation in python
					input_angle = 2 * fpi + input_angle;
				}
				// Debug.Log(input_angle); // testing
				if ( input_angle > fpi / 2 && input_angle <= fpi ) {
					Debug.Log("WARNING, angle xy for legs is restricted to range [-pi/2, pi/2]");
					Debug.Log("angle defaulting to pi/2");
					input_angle = fpi/2;
				}
				else if ( input_angle > fpi && input_angle < 3 * fpi / 2 ) {
					Debug.Log("WARNING, angle xy for legs is restricted to range [-pi/2, pi/2]");
					Debug.Log("angle defaulting to -pi/2");
					input_angle = 3 * fpi / 2;
				}
				plane_angle = input_angle;
				update_upper_joint_coordinates();
			}
		}
		public override float XY { // this is restricted to the range [0, pi/2]
			get {
				return height_angle;
			}
			set {
				float fpi = (float)(System.Math.PI); // float value of pi
				float input_angle = value % (fpi * 2);
				if (input_angle < 0) { // this if statement is necessary if we want to achieve the result we would have from the modulo operation in python
					input_angle = 2 * fpi + input_angle;
				}
				if ( input_angle > (fpi / 2) ) {
					Debug.Log("WARNING, angle xy for legs is restricted to range [0, pi/2]");
					Debug.Log("angle defaulting to pi/2");
					input_angle = (fpi / 2);
				}
				if ( input_angle < 0 ) {
					Debug.Log("WARNING, angle xy for legs is restricted to range [0, pi/2]");
					Debug.Log("angle defaulting to 0");
					input_angle = 0;
				}
				height_angle = input_angle;
				update_upper_joint_coordinates();
			}
		}
		public leg_bone(metaball lower, metaball upper) : base(lower, upper){} //inheritance constructor
	}

	protected class body_bone : bone {
		public override float XZ { // this is restricted to 0
			get {
				return plane_angle;
			}
			set {
				Debug.Log("WARNING, backbone cannot have any XZ angle, defaulting to zero");
				plane_angle = 0;
				update_upper_joint_coordinates();
			}
		}
		public override float XY { // this is restricted to the range [-pi/2, pi/2]
			get {
				return height_angle;
			}
			set {
				float fpi = (float)(System.Math.PI); // float value of pi
				float input_angle = value % (2*fpi);
				if (input_angle < 0) { // this if statement is necessary if we want to achieve the result we would have from the modulo operation in python
					input_angle = 2 * fpi + input_angle;
				}
				if ( input_angle > fpi / 2 && input_angle <= fpi ) {
					Debug.Log("WARNING, angle xy for legs is restricted to range [-pi/2, pi/2]");
					Debug.Log("angle defaulting to pi/2");
					input_angle = fpi/2;
				}
				else if ( input_angle > fpi && input_angle < 3 * fpi / 2 ) {
					Debug.Log("WARNING, angle xy for legs is restricted to range [-pi/2, pi/2]");
					Debug.Log("angle defaulting to -pi/2");
					input_angle = 3 * fpi / 2;
				}
				height_angle = input_angle;
				update_upper_joint_coordinates();
			}
		}
		public body_bone(metaball lower, metaball upper) : base(lower, upper){} //inheritance constructor
	}

	protected class head_bone : bone {
		public override float XZ { // this is restricted to 0
			get {
				return plane_angle;
			}
			set {
				Debug.Log("WARNING, backbone cannot have any XZ angle, defaulting to zero");
				plane_angle = 0;
				update_upper_joint_coordinates();
			}
		}
		public override float XY { // this is restricted to the range [-pi/2, 0]
			get {
				return height_angle;
			}
			set {
				float fpi = (float)(System.Math.PI); // float value of pi
				float input_angle = value % (2*fpi);
				if (input_angle < 0) { // this if statement is necessary if we want to achieve the result we would have from the modulo operation in python
					input_angle = 2 * fpi + input_angle;
				}
				if (input_angle < 3 * fpi / 2 && input_angle != 0 ) {
					Debug.Log("WARNING, angle xy for legs is restricted to range [-pi/2, pi/2]");
					Debug.Log("angle defaulting to -pi/2");
					input_angle = 3 * fpi / 2;
				}
				height_angle = input_angle;
				update_upper_joint_coordinates();
			}
		}
		public head_bone(metaball lower, metaball upper) : base(lower, upper){} //inheritance constructor
	}

	protected class body_part : corporial {
		public bone[] bones; // an array consisting of the bones that connect each of the joints
		public void Update() {
			for ( int i = 0 ; i < bones.Length ; i++ ) {
				bones[i].update_upper_joint_coordinates();
			}
		}
	}

	protected class leg : body_part{
		public leg(int number_of_joints) {
			//leg constructor	
			int no_j = number_of_joints;
			if (!( number_of_joints < 5 && number_of_joints > 1 )) {
				Debug.Log("WARNING: a leg must have between 2 and 4 joints, defaulting to 4");
				no_j = 4;
			}
			// main case
			joints = new metaball[no_j];
			bones = new leg_bone[no_j - 1];
			for ( int i = 0 ; i < joints.Length ; i++ ) {
				joints[i] = new metaball(i*2,0,0,1);
			}
			for ( int i = 0 ; i < bones.Length ; i++ ) {
				// associating each bone with a joint
				bones[i] = new leg_bone(joints[i], joints[i + 1]);
			}
		}
	}

	protected class body : body_part{
		public body(int number_of_joints) {
			//leg constructor	
			int no_j = number_of_joints;
			if (!( number_of_joints < 4 && number_of_joints > 1 )) {
				Debug.Log("WARNING: a body must have between 2 and 3 joints, defaulting to 3");
				no_j = 3;
			}
			// main case
			joints = new metaball[no_j];
			bones = new body_bone[no_j - 1];
			for ( int i = 0 ; i < joints.Length ; i++ ) {
				joints[i] = new metaball(i*2,0,0,1);
			}
			// associating each bone with a joint
			bones[0] = new body_bone(joints[0], joints[1]);
			if (no_j == 3) {
				bones[1] = new body_bone(joints[0], joints[2]);
			}
		}
	}

	protected class head : body_part{
		public head() {
			// main case
			joints = new metaball[2];
			bones = new head_bone[1];
			joints[0] = new metaball(0,0,0,1);
			joints[1] = new metaball(2,0,0,1);
			bones[0] = new head_bone(joints[0], joints[1]);
		}
	}

	protected class animal : organism {
		public int render_mode; //for the purposes of the animal creator program, dictates what parts are drawn ( 0:leg, 1:boy, 2:head, 3:everything)

		public leg hind_leg;
		public body torso;
		public head skull;
		public corporial corpus;

		// public corporial subject;
		protected override void update_subject() {
			subject = hind_leg;
			if ( render_mode == 0 ) {
				subject = hind_leg;
			}
			else if ( render_mode == 1 ) {
				subject = torso;
			}
			else if (render_mode == 2) {
				subject = skull;
			}
			else if (render_mode == 3) {
				subject = corpus;
			}
		}

		public key_animal_data collect_body_parts(float head_height_percentage,float neck_length, int no_legs, float leg_depth_percentage) {
			corpus = null;
			corpus = Centre(torso);

			find_region();
			float x_ordinate_of_torso_front = top_corner.x - max_radius;
			float x_ordinate_of_torso_back = max_radius - bottom_corner.x;
			float leg_depth_factor = System.Math.Min(x_ordinate_of_torso_back,x_ordinate_of_torso_front);
			// float leg_depth_percentage;

			// code for affixing head
			Vector3 base_of_neck = new Vector3(corpus.joints[1].X, corpus.joints[1].Y,corpus.joints[1].Z);
			float radius_of_neck = corpus.joints[1].Radius;
			Vector3 neck = new Vector3(0,0,0);
			neck.z = 0;
			neck.y = ( (head_height_percentage/100) * 2 - 1) * radius_of_neck;
			neck.x = (float)System.Math.Sqrt(radius_of_neck*radius_of_neck - (neck.y)*(neck.y)); //NEW
			neck = Vector3.Normalize(neck);
			neck = neck * neck_length;
			neck = base_of_neck + neck; //NEW
			Vector3 chin = new Vector3(skull.joints[1].X+base_of_neck.x +neck.x,skull.joints[1].Y+base_of_neck.y+neck.y,skull.joints[1].Z+base_of_neck.z);
			var metalist = new List<metaball>();
			for (int i = 0  ; i < corpus.joints.Length ; i++ ) {
				metalist.Add(corpus.joints[i]);
			}
			key_animal_data results = new key_animal_data();
			Vector3 connected_head_centre = new Vector3(base_of_neck.x+neck.x,base_of_neck.y+neck.y,base_of_neck.z);
			results.head_position = connected_head_centre;
			// results.head_position = new Vector3(-connected_head_centre.z, connected_head_centre.y, connected_head_centre.x);			
			results.main_radius = skull.joints[0].Radius;
			results.chin_radius = skull.joints[1].Radius;
			results.main_chin_vertical_distance = (connected_head_centre.y - chin.y);
			metalist.Add(new metaball(base_of_neck.x+neck.x,base_of_neck.y+neck.y,base_of_neck.z,skull.joints[0].Radius));
			metalist.Add(new metaball(chin.x,chin.y,chin.z,skull.joints[1].Radius));

			if ( no_legs > 0 ) {

				//shifting leg so that highest joint is at zero vector
				corporial new_leg = new corporial();
				new_leg.joints = new metaball[hind_leg.joints.Length]; // new leg is a copy of hind_leg but shifted so that the highest joint (ie the shoulder if it had 4 joints) is at (0,0,0)
				Vector3 tmp = new Vector3(0,0,0);
				for ( int i = 0 ; i < hind_leg.joints.Length ; i++ ) {
					tmp = new Vector3(
						hind_leg.joints[i].X,
						hind_leg.joints[i].Y,
						hind_leg.joints[i].Z
					) - new Vector3(
						hind_leg.joints[hind_leg.joints.Length-1].X,
						hind_leg.joints[hind_leg.joints.Length-1].Y,
						hind_leg.joints[hind_leg.joints.Length-1].Z
					);
					new_leg.joints[i] = new metaball(
						tmp.x,
						tmp.y,
						tmp.z,
						hind_leg.joints[i].Radius
					);

				}
				new_leg = anitclockwise_rotate_piontwo(new_leg);
				if ( no_legs < 2 ) {
					leg_depth_percentage = 0;
				}
				//finding base z ordinate for leg, based on the radius of the nearest metaball
				float closest_metaball_radius = 0;
				float distance = Single.MaxValue;
				float tmp_d;
				for ( int i = 0 ; i < metalist.Count ; i++ ) {
					tmp_d = (leg_depth_factor * (leg_depth_percentage/100) - metalist[i].X);
					tmp_d = tmp_d * tmp_d;
					if (tmp_d < distance) {
						distance = tmp_d;
						closest_metaball_radius = metalist[i].Radius;
					}
				}
				Vector3 leg_connector_position = new Vector3(
					new_leg.joints[new_leg.joints.Length - 1].X - leg_depth_factor * (leg_depth_percentage/100),
					new_leg.joints[new_leg.joints.Length - 1].Y,
					new_leg.joints[new_leg.joints.Length - 1].Z - closest_metaball_radius
				);
				for ( int i = 0 ; i < new_leg.joints.Length ; i++ ) {
					new_leg.joints[i].X = new_leg.joints[i].X + leg_connector_position.x;
					new_leg.joints[i].Y = new_leg.joints[i].Y + leg_connector_position.y;
					new_leg.joints[i].Z = new_leg.joints[i].Z + leg_connector_position.z;
					metalist.Add(new_leg.joints[i]);
				}
				var mirror_leg = mirror_in_xy(new_leg);
				for ( int i = 0 ; i < mirror_leg.joints.Length ; i++ ) {
					metalist.Add(mirror_leg.joints[i]);
				}
				if ( no_legs == 2 ) {
					var front_leg = mirror_in_zy(new_leg);
					var front_mirror_leg = mirror_in_zy(mirror_leg);
					for ( int i = 0 ; i < front_leg.joints.Length ; i++ ) {
						metalist.Add(front_leg.joints[i]);
						metalist.Add(front_mirror_leg.joints[i]);
					}
				}
			}
			corpus.joints = metalist.ToArray();		
			// corpus = anitclockwise_rotate_piontwo(corpus);
			// corpus = mirror_in_xy(corpus);
			return results;
		}

		public animal(int no_leg_joints, int no_body_joints) : base() {
			hind_leg = new leg(no_leg_joints);
			torso = new body(no_body_joints);
			skull = new head();
			subject = hind_leg; // the default view is of the leg (this should be changed for the final draft)
		}
	}
}

}
