using System;		
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using connect;

using System.Diagnostics; // TIME TESTING

namespace EcoBuilder.Archie
{
	using Distribution;
	using clamp;

	public class Bonework : bone_and_animal{

		//public interfaces
		//general render options
		public int number_of_marching_cubes = 100000;
		public bool interpolation; 
		public bool optimise; 

		//leg controls
		public float paw_size;
		public float foot_length;
		public float foot_xy;
		public float foot_xz;
		public float wrist_size;

		public float shin_length;
		public float shin_xy;
		public float shin_xz;
		public float knee_size;

		public float thigh_length;
		public float thigh_xy;
		public float thigh_xz;
		public float shoulder_size;

		public float leg_joint_select;

		//body controls
		public float rear_size;
		public float back_length;
		public float back_xy;
		public float front_size;

		public float belly_length;
		public float belly_xy;
		public float belly_size;

		public float body_joint_select;

		//head controls
		public float main_size;
		public float jaw_length;
		public float jaw_xy;
		public float chin_size;
		

		//animal controls
		public float height_percent;
		public float neck_length;
		public float number_of_leg_pairs;
		public float leg_depth;

		//eye interface
		public float eye_height_percent;
		public float eye_width_percent;
		// public GameObject eyes;

		//mouth interface
		public float mouth_height_percent;
		// public GameObject mouth;

		public float mouth_type;
		public float eye_type;


		// public float InSize;


		// local variables
		MeshFilter mesh_filter;
		Oculus eye_controls;
		Maw mouth_controls;
		animal testman;
		bool save;
		key_animal_data eye_details;

		public void Refresh(float size = 1, bool first_time = true)
		{
			size = security.clamp(0,1,size);

			if ( first_time )
			{
				interpolation = true;
				optimise = true;
				save = false;
				eye_details = new key_animal_data();
				mode = 3;
				distribute_parameters(size);
				mesh_filter = GetComponent<MeshFilter>();
				eye_controls = gameObject.transform.Find("eyes").gameObject.GetComponent<Oculus>();
				mouth_controls = gameObject.transform.Find("mouth").gameObject.GetComponent<Maw>();
				testman = new animal((int)leg_joint_select , (int)body_joint_select );
				Stopwatch parameter_insert_time = new Stopwatch(); // TIME TESTING
				parameter_insert_time.Start();// TIME TESTING

				testman.render_mode = mode;
				float degrees_to_radians_scalefactor = ((float)(System.Math.PI * 2) / (360));
				testman.hind_leg.bones[0].XY = foot_xy * degrees_to_radians_scalefactor;
				testman.hind_leg.bones[0].XZ = foot_xz * degrees_to_radians_scalefactor;
				testman.hind_leg.bones[0].Length = foot_length ;
				testman.hind_leg.joints[0].Radius = paw_size ;
				testman.hind_leg.joints[1].Radius = wrist_size ;
				if (leg_joint_select  > 2) {
					// enable shin slider
					testman.hind_leg.bones[1].XY = shin_xy * degrees_to_radians_scalefactor;
					testman.hind_leg.bones[1].XZ = shin_xz * degrees_to_radians_scalefactor;
					testman.hind_leg.bones[1].Length = shin_length ;
					testman.hind_leg.joints[2].Radius = knee_size ;
				}
				if (leg_joint_select  == 4) {
					testman.hind_leg.bones[2].XY = thigh_xy * degrees_to_radians_scalefactor;
					testman.hind_leg.bones[2].XZ = thigh_xz * degrees_to_radians_scalefactor;
					testman.hind_leg.bones[2].Length = thigh_length ;
					testman.hind_leg.joints[3].Radius = shoulder_size ;
					// enable foot, shin and thigh sliders
				}
				testman.torso.bones[0].XY = back_xy * degrees_to_radians_scalefactor;
				testman.torso.bones[0].Length = back_length ;
				testman.torso.joints[0].Radius = rear_size ;
				testman.torso.joints[1].Radius = front_size ;
				if (body_joint_select  == 3) {
					testman.torso.bones[1].XY = belly_xy * degrees_to_radians_scalefactor;
					testman.torso.bones[1].Length = belly_length ;
					testman.torso.joints[2].Radius = belly_size ;
				}
				testman.skull.joints[0].Radius = main_size ;
				testman.skull.bones[0].XY = jaw_xy * degrees_to_radians_scalefactor;
				testman.skull.bones[0].Length = jaw_length ;
				testman.skull.joints[1].Radius = chin_size ;
				eye_details = testman.collect_body_parts(height_percent , neck_length , (int)number_of_leg_pairs , leg_depth );

				parameter_insert_time.Stop(); // TIME TESTING
				UnityEngine.Debug.Log("parameter insertion time: " + parameter_insert_time.Elapsed); // TIME TESTING
			}


            // placing eye, finding eye coordinates
	        Stopwatch eye_placing_time = new Stopwatch(); // TIME TESTING
            eye_placing_time.Start();// TIME TESTING

            // binary search
            var source = eye_details;
            float y = (eye_height_percent / 100) * (source.main_radius + source.main_chin_vertical_distance - source.chin_radius) + source.head_position.y - source.main_chin_vertical_distance + source.chin_radius;
            float z = (eye_width_percent / 100) * Mathf.Sqrt(Mathf.Pow(source.main_radius, 2) - Mathf.Pow(y - source.head_position.y, 2));
            float x = source.head_position.x + Mathf.Sqrt(Mathf.Pow(source.main_radius, 2) - Mathf.Pow(z, 2));
            Vector3 position = new Vector3(x, y, z);
            float in_or_out = Single.MaxValue;
            Vector3 extreme = position + new Vector3(eye_details.main_radius * 10, 0, 0);
            Vector3 middle;
            for (int h = 0; h < 100; h++)
            {
                middle = (extreme - position) / 2 + position;
                in_or_out = 0;
                for (int i = 0; i < testman.corpus.joints.Length; i++)
                {
                    in_or_out = in_or_out + testman.corpus.joints[i].evaluated_distance(middle);
                }
                if (in_or_out < 1)
                {
                    extreme = middle;
                }
                else
                {
                    position = middle;
                }
            }
            eye_controls.Refresh(position, 1.0F - 0.5F * size, (int)eye_type, false, first_time);


            eye_placing_time.Stop(); // TIME TESTING
            UnityEngine.Debug.Log("eye placing time: " + eye_placing_time.Elapsed); // TIME TESTING



            //placing mouth

            Stopwatch mouth_placing_time = new Stopwatch(); // TIME TESTING
            mouth_placing_time.Start();// TIME TESTING

            // binary search
            Stopwatch mouth_search_time = new Stopwatch(); // TIME TESTING
            mouth_search_time.Start();// TIME TESTING
            y = source.head_position.y - source.main_chin_vertical_distance;
            y = ((position.y - y) * mouth_height_percent / 100) + y;
            z = 0;
            x = source.head_position.x;
            position = new Vector3(x, y, z);
            in_or_out = Single.MaxValue;
            extreme = position + new Vector3(eye_details.main_radius * 10, 0, 0);
            for (int h = 0; h < 100; h++)
            {
                middle = (extreme - position) / 2 + position;
                in_or_out = 0;
                for (int i = 0; i < testman.corpus.joints.Length; i++)
                {
                    in_or_out = in_or_out + testman.corpus.joints[i].evaluated_distance(middle);
                }
                if (in_or_out < 1)
                {
                    extreme = middle;
                }
                else
                {
                    position = middle;
                }
            }
            mouth_search_time.Stop(); // TIME TESTING
            UnityEngine.Debug.Log("mouth search time: " + mouth_search_time.Elapsed); // TIME TESTING


            Stopwatch mouth_refresh_time = new Stopwatch(); // TIME TESTING
			mouth_refresh_time.Start();// TIME TESTING
			mouth_controls.Refresh(position, 1.0F - 0.5F * size, (int)mouth_type, first_time);
			mouth_refresh_time.Stop(); // TIME TESTING
			UnityEngine.Debug.Log("mouth refresh time: " + mouth_refresh_time.Elapsed); // TIME TESTING

			mouth_placing_time.Stop(); // TIME TESTING
			UnityEngine.Debug.Log("mouth placing time: " + mouth_placing_time.Elapsed); // TIME TESTING


			if (first_time)
			{
				Stopwatch region_finding_time = new Stopwatch(); // TIME TESTING
				region_finding_time.Start();// TIME TESTING
				testman.find_region();
				region_finding_time.Stop(); // TIME TESTING
				UnityEngine.Debug.Log("region finding time: " + region_finding_time.Elapsed); // TIME TESTING

				Stopwatch region_partision_time = new Stopwatch(); // TIME TESTING
				region_partision_time.Start();// TIME TESTING
				testman.partision_region(number_of_marching_cubes);
				region_partision_time.Stop(); // TIME TESTING
				UnityEngine.Debug.Log("region partision time: " + region_partision_time.Elapsed); // TIME TESTING

				testman.interpolation = interpolation;
				testman.optimisation = optimise;
				Stopwatch mesh_making_time = new Stopwatch(); // TIME TESTING
				mesh_making_time.Start();// TIME TESTING
				testman.make_mesh();
				mesh_making_time.Stop(); // TIME TESTING
				UnityEngine.Debug.Log("mesh making time: " + mesh_making_time.Elapsed); // TIME TESTING

				mesh_filter.mesh = testman.animal_mesh;
                float volume = mesh_filter.mesh.bounds.size.x * mesh_filter.mesh.bounds.size.y * mesh_filter.mesh.bounds.size.z;
                float dim = (float)System.Math.Pow(volume, (1.0f / 3.0f));
                float scale_factor = 1.0F / dim;
                transform.localScale = new Vector3(scale_factor, scale_factor, scale_factor);
            }

			if ( save ) {
				// Debug.Log("Saving animal to file...");
				testman.save_mesh();			
			}
			save = false;

		}

		public int mode = 3;
		
		public void animal_mode() {
			mode = 3;
			Refresh();
		}
		public void save_animal() {
			// Debug.Log("ding...");
			save = true;
			Refresh();
		}


		public void distribute_parameters(float size = 1) 
		{
			Stopwatch distribution_time = new Stopwatch(); // TIME TESTING
			distribution_time.Start();// TIME TESTING

			size = security.clamp(0,1,size);

			paw_size  = normal.Random(0.5F + 0.5F * size, 0.2F); //NEW
			foot_length  = normal.Random(1 + size, 0.2F); //NEW
			foot_xy  = normal.Random(45, 30, true);
			foot_xz  = normal.Random(0, 45);
			wrist_size  = normal.Random(paw_size, 0.2F);

			shin_length  = normal.Random(wrist_size * 3, 0.2F);
			shin_xy  = normal.Random(45, 30, true);
			shin_xz  = normal.Random(0, 45);
			knee_size  = normal.Random(wrist_size, 0.2F);
			// shin_controls;

			thigh_length  = normal.Random(knee_size * 3, 0.2F);
			thigh_xy  = normal.Random(45, 30, true);
			thigh_xz  = normal.Random(0, 45);
			shoulder_size  = normal.Random(knee_size, 0.2F);
			// thigh_controls;

			leg_joint_select  = UnityEngine.Random.Range(2, 5);
			// all_leg_controls;

			//body controls
			rear_size  = normal.Random(shoulder_size * 2, 0.5F);
			back_length  = normal.Random(shoulder_size * 7, 0.5F);
			back_xy  = normal.Random(0, 10);
			front_size  = normal.Random(shoulder_size * 2, 0.5F);
			back_length = security.clamp(0.0f, (front_size + rear_size), back_length);

			belly_length  = normal.Random(shoulder_size * 5, 0.5F);
			belly_xy  = normal.Random(0, 30);
			belly_size  = normal.Random(shoulder_size * 2, 0.5F);
			// belly_controls;

			body_joint_select  = UnityEngine.Random.Range(2, 4);
			// all_body_controls;

			//head controls
			main_size  = normal.Random(paw_size * 1.5F, 0.2F);
			jaw_length  = normal.Random(main_size * 0.5F, 0.2F);
			jaw_xy  = (-1) * (normal.Random(45, 30, true)); // stricktly negative
			chin_size  = normal.Random(main_size * 0.5F, 0.2F);

			// all_head_controls;

			//animal controls
			height_percent  = normal.Random(50, 15);
			neck_length  = normal.Random(front_size * 0.25F, 0.5F, true);//NEW
			neck_length = security.clamp(0.0f, front_size + main_size, neck_length);
			// UnityEngine.Debug.Log("neck body ratio: " + neck_length/front_size);//testing
			// UnityEngine.Debug.Log("neck head ratio: " + neck_length/main_size);//testing
			// neck_length = 0;//testing
			// jaw_length = 0;//testing
			float dice = UnityEngine.Random.Range(0, 100);
			if ( dice < 10 ) {
				number_of_leg_pairs  = 0;
			}
			else if ( dice < 50 ) {
				number_of_leg_pairs  = 1;
			}
			else {
				number_of_leg_pairs  = 2;
			}
			leg_depth  = normal.Random(50, 15);
			// all_animal_controls;

			//eye interface
			eye_height_percent  = normal.Random(50, 15);
			eye_width_percent  = normal.Random(50, 15);

			//mouth interface
			mouth_height_percent = normal.Random(50, 15);

			mouth_type  = UnityEngine.Random.Range(0, 3);
			mouth_type = security.clamp(0, 2, mouth_type);
			eye_type  = UnityEngine.Random.Range(0, 8);
			eye_type = security.clamp(0, 7, eye_type);

			distribution_time.Stop(); // TIME TESTING
			UnityEngine.Debug.Log("distribution time: " + distribution_time.Elapsed); // TIME TESTING
		}
	}
}