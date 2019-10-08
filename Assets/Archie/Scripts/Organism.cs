using System;		
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace EcoBuilder.Archie
{

    public class Organism : marching_cubes
{
	protected class corporial{
		public metaball[] joints;	// array containing the metaballs that make up a leg's joints
	}

	class ray {
		private Vector3 current_ray_position;
		private Vector3 end_position;
		private float resolution;

		private Vector3 direction_of_ray;

		public static float evaluate( Vector3 point, corporial subject ) {
			float distance_value = 0;
			for ( int i = 0 ; i < subject.joints.Length ; i++ ) {
				distance_value = distance_value + subject.joints[i].evaluated_distance(point);
			}
			return distance_value;
		}

		public bool cast(corporial subject) {
			// moves the ray forwards by one step and returns if the new position is inside the object
			current_ray_position = current_ray_position + direction_of_ray * resolution;
			return ( evaluate(current_ray_position, subject) >= 1 ) ;
		}

		public ray(Vector3 start_position, Vector3 end, int number_of_steps) {
			resolution = (end - start_position).magnitude/(float)number_of_steps;
			current_ray_position = start_position;
			end_position = end;
			direction_of_ray = Vector3.Normalize( end - start_position );
		}
	}

	protected class organism {
		public corporial subject;
		protected virtual void update_subject() {
			// subject = subject;
		}

		protected List<marching_cube> set_of_cubes = new List<marching_cube>();
		protected int set_of_cubes_count;

		protected float max_radius;

		protected Vector3 top_corner;
		protected Vector3 bottom_corner;

		protected int resolution;
		protected float size_of_marching_cubes;

		protected int x_width_in_cubes;
		protected int y_width_in_cubes;
		protected int z_width_in_cubes;
		

		public Mesh animal_mesh;

		public bool interpolation;
		public bool optimisation;

		protected VertexArray vertex_registry;
		//three dimensional array of ints
		// one uses this array to querry if another cube already has a vertex at this point
		// if it does, the index of the cube in set_of_cubes is returned and which vertex it is
		// in a vector2 data type, first element cube index, second element vertex index

		//corporial manipulation functions
		protected static Vector3 CoM(metaball[] component_parts, bool weighted = true){
			Vector3 com = new Vector3(0,0,0);
			float sum_of_sphere_volumes = 0;
			float sphere_volume;
			for ( int i = 0 ; i < component_parts.Length ; i++ ) {
				if ( weighted ) {
					sphere_volume = (float)((4/3)*System.Math.PI*System.Math.Pow(component_parts[i].Radius,3));
					com = com + new Vector3(component_parts[i].X, component_parts[i].Y, component_parts[i].Z) * sphere_volume;
					sum_of_sphere_volumes = sum_of_sphere_volumes + sphere_volume;
				}
				else {
					com = com + new Vector3(component_parts[i].X, component_parts[i].Y, component_parts[i].Z);
				}
			}
			if (weighted) {
				com = com / sum_of_sphere_volumes;
			}
			else {
				com = com / component_parts.Length;
			}
			return com;
		}

		protected static corporial Centre(corporial mass){
			corporial centred_mass = new corporial();
			centred_mass.joints = new metaball[mass.joints.Length];
			Vector3 mass_centre_of_mass =  CoM(mass.joints);
			Vector3 difference;
			for( int i = 0 ; i < mass.joints.Length ; i++ ) {
				difference = new Vector3(mass.joints[i].X,mass.joints[i].Y,mass.joints[i].Z) - mass_centre_of_mass;
				centred_mass.joints[i] = new metaball(difference.x, difference.y, difference.z, mass.joints[i].Radius);
			}
			return centred_mass;
		}

		protected static corporial anitclockwise_rotate_piontwo(corporial mass){
			var rotated_mass = new corporial();
			rotated_mass.joints = new metaball[mass.joints.Length];
			for ( int i = 0 ; i < mass.joints.Length ; i++ ) {
				rotated_mass.joints[i] = new metaball(
					-mass.joints[i].Z,
					mass.joints[i].Y,
					mass.joints[i].X,
					mass.joints[i].Radius
				);
			}
			return rotated_mass;
		}
		protected static corporial mirror_in_xy(corporial mass){
			var mirrored_mass = new corporial();
			mirrored_mass.joints = new metaball[mass.joints.Length];
			for ( int i = 0 ; i < mass.joints.Length ; i++ ) {
				mirrored_mass.joints[i] = new metaball(
					mass.joints[i].X,
					mass.joints[i].Y,
					-mass.joints[i].Z,
					mass.joints[i].Radius
				);
			}
			return mirrored_mass;
		}
		protected static corporial mirror_in_zy(corporial mass){
			var mirrored_mass = new corporial();
			mirrored_mass.joints = new metaball[mass.joints.Length];
			for ( int i = 0 ; i < mass.joints.Length ; i++ ) {
				mirrored_mass.joints[i] = new metaball(
					-mass.joints[i].X,
					mass.joints[i].Y,
					mass.joints[i].Z,
					mass.joints[i].Radius
				);
			}
			return mirrored_mass;
		}

		protected static float calculated_cube_size(corporial rendered_object, int target_number_of_vertices){
			int number_of_metaballs = rendered_object.joints.Length;
			float surface_area;
			float sum_surface_area = 0;
			float sum_radius = 0;
			for ( int i = 0 ; i < number_of_metaballs ; i++ ) {
				surface_area = (float)(4*System.Math.PI*System.Math.Pow(rendered_object.joints[i].Radius,2));
				sum_surface_area = sum_surface_area + surface_area;
				sum_radius = sum_radius + rendered_object.joints[i].Radius;
			}
			float method1 = (float)System.Math.Sqrt(sum_surface_area / target_number_of_vertices);
			float surface = (float)(4*System.Math.PI*System.Math.Pow(sum_radius,2));
			float method2 = (float)System.Math.Sqrt(surface / target_number_of_vertices);
			// return (method1+method2)/2;
			return method1;
		}
		//method 1 tends to make the cubes too big
		// method 2 tends to make the cubes too small



		private Vector3 push_to_edge(Vector3 calculated_corner, float max_radius, corporial subject, bool positive) {
			float distance_value = ray.evaluate(calculated_corner, subject);
			if ( distance_value >= 1 ) {
				max_radius = max_radius * 0.1F;
				if ( positive ) {
					calculated_corner = calculated_corner + new Vector3(max_radius,max_radius,max_radius);
				}
				else {
					calculated_corner = calculated_corner - new Vector3(max_radius,max_radius,max_radius);
				}
				calculated_corner = push_to_edge(calculated_corner, max_radius, subject, positive);
			}
			return calculated_corner;
		}

		public void find_region(bool account_for_overlap = false) {

			int similar_counter;
			update_subject();
			top_corner = new Vector3(subject.joints[0].X, subject.joints[0].Y, subject.joints[0].Z);
			bottom_corner = new Vector3(subject.joints[0].X, subject.joints[0].Y, subject.joints[0].Z);
			max_radius = subject.joints[0].Radius;
			for ( int i = 1 ; i < subject.joints.Length ; i++ ) {
				if ( subject.joints[i].X > top_corner.x ) {
					top_corner.x = subject.joints[i].X;
				}
				if ( subject.joints[i].Y > top_corner.y ) {
					top_corner.y = subject.joints[i].Y;
				}
				if ( subject.joints[i].Z > top_corner.z ) {
					top_corner.z = subject.joints[i].Z;
				}
				if ( subject.joints[i].Radius > max_radius ) {
					max_radius = subject.joints[i].Radius;
				}
				if ( subject.joints[i].X < bottom_corner.x ) {
					bottom_corner.x = subject.joints[i].X;
				}
				if ( subject.joints[i].Y < bottom_corner.y ) {
					bottom_corner.y = subject.joints[i].Y;
				}
				if ( subject.joints[i].Z < bottom_corner.z ) {
					bottom_corner.z = subject.joints[i].Z;
				}
				if ( account_for_overlap ) {
					similar_counter = 0;
					for ( int j = 0 ; j < subject.joints.Length ; j++ ) {
						if ( (new Vector3(subject.joints[j].X,subject.joints[j].Y,subject.joints[j].Z) - new Vector3(subject.joints[i].X,subject.joints[i].Y,subject.joints[i].Z)).magnitude <=subject.joints[i].Radius + subject.joints[j].Radius ) {
							similar_counter++;
						}
						if ( (subject.joints[i].Radius + subject.joints[j].Radius) * similar_counter > max_radius ) {
							max_radius = (subject.joints[i].Radius + subject.joints[j].Radius) * similar_counter;
						}
					}
				}
			}

			bool upper_collision = true;
			bool lower_collision = true;
			// bool bottom_inside = true, top_inside = true;
			while ( lower_collision && upper_collision ) {
				max_radius = max_radius * 2;
				if ( upper_collision ) {
					top_corner = top_corner + new Vector3(max_radius,max_radius,max_radius);
				}
				if (lower_collision) {
					bottom_corner = bottom_corner - new Vector3(max_radius,max_radius,max_radius);
				}

				int number_of_steps = 10;
				var casting_rays = new ray[12];
				casting_rays[0] = new ray(bottom_corner, new Vector3(top_corner.x, bottom_corner.y, bottom_corner.z), number_of_steps);
				casting_rays[1] = new ray(bottom_corner, new Vector3(bottom_corner.x, bottom_corner.y, top_corner.z), number_of_steps);
				casting_rays[2] = new ray(bottom_corner, new Vector3(bottom_corner.x, top_corner.y, bottom_corner.z), number_of_steps);
				casting_rays[3] = new ray(top_corner, new Vector3(bottom_corner.x, top_corner.y, top_corner.z), number_of_steps);
				casting_rays[4] = new ray(top_corner, new Vector3(top_corner.x, top_corner.y, bottom_corner.z), number_of_steps);
				casting_rays[5] = new ray(top_corner, new Vector3(top_corner.x, bottom_corner.y, top_corner.z), number_of_steps);
				casting_rays[6] = new ray(top_corner, new Vector3(top_corner.x, bottom_corner.y, bottom_corner.z), number_of_steps);
				casting_rays[7] = new ray(top_corner, new Vector3(bottom_corner.x, bottom_corner.y, top_corner.z), number_of_steps);
				casting_rays[8] = new ray(top_corner, new Vector3(bottom_corner.x, top_corner.y, bottom_corner.z), number_of_steps);
				casting_rays[9] = new ray(bottom_corner, new Vector3(bottom_corner.x, top_corner.y, top_corner.z), number_of_steps);
				casting_rays[10] = new ray(bottom_corner, new Vector3(top_corner.x, top_corner.y, bottom_corner.z), number_of_steps);
				casting_rays[11] = new ray(bottom_corner, new Vector3(top_corner.x, bottom_corner.y, top_corner.z), number_of_steps);

				upper_collision = false;
				lower_collision = false;
				for ( int s = 0 ; s < number_of_steps ; s++ ) {
					for ( int i = 0 ; i < 12 ; i++ ) {
						if ( i < 3 ) {
							upper_collision = upper_collision || casting_rays[i].cast(subject);
						}
						else {
							lower_collision = lower_collision || casting_rays[i].cast(subject);
						}
					}
				}
			} 
		}

		public void partision_region(int number_of_marching_cubes) {

			// new code for automatically calculating size of marching cubes
			update_subject();
			float volume = (top_corner.x - bottom_corner.x) * (top_corner.y - bottom_corner.y) * (top_corner.z - bottom_corner.z);
			float cube_volume = volume/(float)number_of_marching_cubes;
            float cube_dimensions = (float)System.Math.Pow(cube_volume, (1.0/3.0));
			size_of_marching_cubes = cube_dimensions;
            // size_of_marching_cubes = calculated_cube_size(subject, resolution_in_vertices); //OLDER

            x_width_in_cubes = (int)System.Math.Ceiling((top_corner.x - bottom_corner.x) / size_of_marching_cubes);
			y_width_in_cubes = (int)System.Math.Ceiling((top_corner.y - bottom_corner.y) / size_of_marching_cubes);
			z_width_in_cubes = (int)System.Math.Ceiling((top_corner.z - bottom_corner.z) / size_of_marching_cubes);

			set_of_cubes_count = 0;
			for ( int z = 0 ; z < z_width_in_cubes ; z++ ) {
			// for (int z = 0; z < (float)z_width_in_cubes/2.0f; z++) {
            	for ( int y = 0 ; y < y_width_in_cubes ; y++ ) {
					for ( int x = 0 ; x < x_width_in_cubes ; x++ ) {

						set_of_cubes_count++;
						set_of_cubes.Add(new marching_cube( new Vector3(bottom_corner.x + x * size_of_marching_cubes, bottom_corner.y + y * size_of_marching_cubes, bottom_corner.z + z * size_of_marching_cubes) , size_of_marching_cubes));
					}
				}
			}
		}

		public void make_mesh() {
			update_subject();
			var triangle_list = new List<int>();
			for ( int i = 0 ; i < set_of_cubes_count ; i++ ) {
				set_of_cubes[i].make_triangles_optimised_plus(subject.joints, set_of_cubes, i, bottom_corner, vertex_registry, interpolation);
				for ( int j = 0 ; j < set_of_cubes[i].triangle_interface.Length ; j++ ) {
					triangle_list.Add(set_of_cubes[i].triangle_interface[j]);
				}
			}
			animal_mesh.vertices = vertex_registry.ToVertex();
			animal_mesh.triangles = triangle_list.ToArray();

			//making uv maps
			var uvs = new Vector2[animal_mesh.vertices.Length];
			for ( int i = 0 ; i < animal_mesh.vertices.Length ; i++ ) {
				var diagonal = top_corner - bottom_corner;
				uvs[i] = new Vector2((animal_mesh.vertices[i].x - bottom_corner.x) / diagonal.x, (animal_mesh.vertices[i].y - bottom_corner.y) / diagonal.y); // somehow it now works???
				// uvs[i] = new Vector2((animal_mesh.vertices[i].z - bottom_corner.z) / diagonal.z, (animal_mesh.vertices[i].y - bottom_corner.y) / diagonal.y); // somehow it now works???
			}
			animal_mesh.uv = uvs;
			animal_mesh.RecalculateNormals();
			animal_mesh.RecalculateTangents();
		}

		public void save_mesh() {
			#if UNITY_EDITOR
            AssetDatabase.CreateAsset( animal_mesh, "Assets/saved_animal" ); //saves to root of project file, NB: it does this by creating a text file with the details of the unity gameobject (can't open in blender)
			AssetDatabase.SaveAssets();
			#endif
		}

		public organism() {
			animal_mesh = new Mesh();
			vertex_registry = new VertexArray();
			subject = new corporial();
		}
	}
}

}