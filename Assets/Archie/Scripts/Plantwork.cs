//Plantwork, using L system based technics to create 3d models for plants
//based on the code I wrote for L systems in 2d, in python (see same github repo, l.py)
using System;		
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

using System.Diagnostics; // TIME TESTING

namespace EcoBuilder.Archie
{

    class Plantwork : Organism {

    class rule{
        protected string in_word;
        protected string out_word;

        public virtual string pass_string(string system) {
            return system.Replace(in_word,out_word);
        }

        public rule(string key, string output) {
            in_word = key;
            out_word = output;
        }
    }

    class probabilistic_rule : rule {

        private string substitutes = "XYZxYz";

        public override string pass_string(string system) {
            string new_out_word;
            float dice_role;
            int index = 0;
            var new_string = new System.Text.StringBuilder(out_word);
            for ( int i = 0 ; i < out_word.Length ; i++ ) {
                dice_role = UnityEngine.Random.Range(0,3);
                if ( dice_role < 1 ) {
                    index = 0;
                }
                else if ( dice_role < 2 ) {
                    index = 1;
                }
                else {
                    index = 2;
                }
                if ( out_word[i] == '-' ) {
                    index = index + 3;
                    new_string[i] = substitutes[index];
                }
                else if ( out_word[i] == '+' ) {
                    new_string[i] = substitutes[index];
                }
            }
            new_out_word = new_string.ToString();
            return system.Replace(in_word,new_out_word);
        }

        public probabilistic_rule(string key, string output) : base(key, output) {}
    }

    class l_system {
        private string system;
        public string System {
            get
            {
                return system;
            }
        }
        private List<rule> rules;
        private int iteration;

        public void add_rule(string key, string output, bool probabilistic = false) {
            if ( !probabilistic ) {
                rules.Add(new rule(key, output));
            }
            else {
                rules.Add(new probabilistic_rule(key, output));
            }
        }

        public string update() {
            for ( int i = 0 ; i < rules.Count ; i++ ) {
                system = rules[i].pass_string(system);
            }
            iteration++;
            return system;
        }

        public l_system(string axiom) {
            system = axiom;
            iteration = 0;
            rules = new List<rule>();
        }
    }

    class plant : organism {
        private l_system generator;
        private Vector3 direction;
        private float distance;
        private int slenderness;
        private bool growing;

        public void draw( int iterations ) {
            Stopwatch grammar_processing_time = new Stopwatch(); // TIME TESTING
            grammar_processing_time.Start();// TIME TESTING
            var history_of_branches = new Stack<Vector3>();

            if ( iterations <= 0 ) {
                UnityEngine.Debug.Log("warning, iteration of the l system must be an interager greater than zero");
                return;
            }
            
            var position = new Vector3(0,0,0);
            direction = new Vector3(0,1,0);
            distance = 0.14f * slenderness;

            // distance = 0.7f;


            string system = "";
            if ( growing )
            {
                for ( int i = 0 ; i < iterations ; i++ )
                {
                    system = generator.update();
                }
            }
            else
            {
                system = generator.System;
            }
            grammar_processing_time.Stop(); // TIME TESTING
            UnityEngine.Debug.Log("grammar time: " + grammar_processing_time.Elapsed); // TIME TESTING
            UnityEngine.Debug.Log(system);
            Stopwatch placing_metaballs = new Stopwatch(); // TIME TESTING
            placing_metaballs.Start();// TIME TESTING
            var list_of_points = new List<metaball>();
            // float branch_radius = 1;
            float branch_radius = (2.0f/3.0f)*distance;
            var saved_branch_radius = new Stack<float>();
            var saved_branch_length = new Stack<float>();
            for ( int i = 0 ; i < system.Length ; i++ ) {
                if ('F' == system[i]) {
                    for ( int j = 0; j < slenderness ; j++ ) {
                        position = position + direction * distance/slenderness;
                        list_of_points.Add(new metaball(position.x, position.y, position.z, branch_radius/slenderness)); // note that the radius should decrease, the 1 here is ONLY a PLACEHOLDER
                        distance = distance * 0.99F; // works quite well
                        branch_radius = branch_radius * 0.97F;// works quite well
                    }
                }
                if ('[' == system[i]) {
                    history_of_branches.Push(position);
                    saved_branch_radius.Push(branch_radius);
                    saved_branch_length.Push(distance);
                }
                if (']' == system[i]) {
                    position = history_of_branches.Pop();
                    branch_radius = saved_branch_radius.Pop();
                    distance = saved_branch_length.Pop();
                }
                if ('X' == system[i]) {
                    direction = direction + new Vector3(1,0,0);
                    direction = Vector3.Normalize(direction);
                }
                if ('x' == system[i]) {
                    direction = direction + new Vector3(-1,0,0);
                    direction = Vector3.Normalize(direction);
                }
                if ('Y' == system[i]) {
                    direction = direction + new Vector3(0,1,0);
                    direction = Vector3.Normalize(direction);
                }
                if ('y' == system[i]) {
                    direction = direction + new Vector3(0,-1,0);
                    direction = Vector3.Normalize(direction);
                }
                if ('Z' == system[i]) {
                    direction = direction + new Vector3(0,0,1);
                    direction = Vector3.Normalize(direction);
                }
                if ('z' == system[i]) {
                    direction = direction + new Vector3(0,0,-1);
                    direction = Vector3.Normalize(direction);
                }
            }
            subject.joints = list_of_points.ToArray();
            placing_metaballs.Stop(); // TIME TESTING
            UnityEngine.Debug.Log("placing metaball time: " + placing_metaballs.Elapsed); // TIME TESTING
            Stopwatch find_region_time = new Stopwatch(); // TIME TESTING
            find_region_time.Start();// TIME TESTING
            find_region();
            find_region_time.Stop(); // TIME TESTING
            UnityEngine.Debug.Log("finding region time: " + find_region_time.Elapsed); // TIME TESTING
            Stopwatch partision_region_time = new Stopwatch(); // TIME TESTING
            partision_region_time.Start();// TIME TESTING
            partision_region(5000);
            partision_region_time.Stop(); // TIME TESTING
            UnityEngine.Debug.Log("partision region time: " + partision_region_time.Elapsed); // TIME TESTING
            // partision_region(400);
            // partision_region(100);
            Stopwatch mesh_making_time = new Stopwatch(); // TIME TESTING
            mesh_making_time.Start();// TIME TESTING
            make_mesh();
            mesh_making_time.Stop(); // TIME TESTING
            UnityEngine.Debug.Log("mesh making time: " + mesh_making_time.Elapsed); // TIME TESTING

        }

        public plant(float size = 1, string state = "" ) : base()
        {
            if ( state == "" )
            {
                growing = true;
                float dice_role = UnityEngine.Random.Range(0, 3);
                if ( dice_role < 1 )
                {
                    // Plane Tree
                    UnityEngine.Debug.Log("Tree");
                    generator = new l_system("F");
                    generator.add_rule("F","FF+[+F-F-F]-[-F+F+F]", true); // true indicates this is a probabilistic rule and so will have random elements ( random change in angle )
                }
                else if (dice_role < 2)
                {
                    // L weed
                    UnityEngine.Debug.Log("Weed");
                    generator = new l_system("F");
                    generator.add_rule("F", "FF-[XY]+[XY]", true);
                    generator.add_rule("X", "+FY", true);
                    generator.add_rule("Y", "-FX", true);
                }
                else
                {
                    // L Stick
                    UnityEngine.Debug.Log("Stick");
                    generator = new l_system("X");
                    generator.add_rule("F", "FF", true);
                    generator.add_rule("X", "F[+X]F[-X]+X", true);
                }
                // else {
                //     // Saupe Bush
                //     // the form it was given from the website, conflicted with my system, so have I have made changes
                //     // Y -> A
                //     // X -> B
                //     Debug.Log("Saupe Bush");
                //     generator = new l_system("VZFFF");
                //     generator.add_rule("V", "[+++W][---W]AV", true);
                //     generator.add_rule("W", "+B[-W]Z", true);
                //     generator.add_rule("B", "-W[+B]Z", true);
                //     generator.add_rule("A", "AZ", true);
                //     generator.add_rule("Z", "[-FFF][+FFF]F", true);
                //     // in all my testing, Saupe Bushes ( at least at the early iterations, the only ones my metaball program can render) look like eges and cause matching cube clipping problems
                //     // for this reason I am excluding them
                // }
            }
            else
            {
                growing = false;
                generator = new l_system(state);
            }
            // slenderness = (int)((float)5 * size);

            slenderness = (int)(4.0F * size + 1.0F);
        }
    }


    // local variables
    MeshFilter mesh_filter;
    Pots pot;
	plant cactus;
	bool save;

	public void Refresh(float size = 1, bool first_time = true) {
        if ( first_time ) 
        {
            save = false;
            mesh_filter = GetComponent<MeshFilter>();
            pot = gameObject.transform.Find("pot").gameObject.GetComponent<Pots>(); // note that the transform.Find is immportant (standard Find game object finds highest in hierachy - not necessarily child)
        }

        Stopwatch pot_creation_time = new Stopwatch(); // TIME TESTING
        pot_creation_time.Start();// TIME TESTING
        pot.Refresh(size, first_time);
        pot_creation_time.Stop(); // TIME TESTING
        UnityEngine.Debug.Log("pot creation time: " + pot_creation_time.Elapsed); // TIME TESTING


        Stopwatch plant_creation_time = new Stopwatch(); // TIME TESTING
        plant_creation_time.Start();// TIME TESTING
		cactus = new plant(size);
        plant_creation_time.Stop(); // TIME TESTING
        UnityEngine.Debug.Log("plant creation time: " + plant_creation_time.Elapsed); // TIME TESTING


        cactus.interpolation = true;
		cactus.optimisation = true;

        Stopwatch plant_growing_time = new Stopwatch(); // TIME TESTING
        plant_growing_time.Start();// TIME TESTING
        // cactus.draw(3); // WARNING, 3 does load but takes a very long time
        cactus.draw(2); // WARNING, 3 does load but takes a very long time
        plant_growing_time.Stop(); // TIME TESTING
        UnityEngine.Debug.Log("plant growing time: " + plant_growing_time.Elapsed); // TIME TESTING

        mesh_filter.mesh = cactus.animal_mesh;
		if ( save ) {
			// Debug.Log("Saving animal to file...");
			cactus.save_mesh();
		}
		save = false;

        float volume = mesh_filter.mesh.bounds.size.x * mesh_filter.mesh.bounds.size.y * mesh_filter.mesh.bounds.size.z;
        float dim = (float)System.Math.Pow(volume, (1.0f / 3.0f));
        float scale_factor = 1.0F / dim;
        transform.localScale = new Vector3(scale_factor, scale_factor, scale_factor);
	}	

	public void save_animal() {
		// Debug.Log("ding...");
		save = true;
		Refresh();
	}
}

}