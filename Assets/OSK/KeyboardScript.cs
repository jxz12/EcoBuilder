using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OSK
{
    public class KeyboardScript : MonoBehaviour
    {
        [SerializeField] InputField TextField;
        [SerializeField] GameObject EngLayoutSml, EngLayoutBig, SymbLayout;

        public void SetTextField(InputField newField)
        {
            if (newField!=null)
            {
                GetComponent<RectTransform>().pivot = new Vector2(.5f,0);
                GetComponent<Canvas>().enabled = true;
            }
            else
            {
                GetComponent<RectTransform>().pivot = new Vector2(.5f,0);
                GetComponent<Canvas>().enabled = true;
            }
            TextField = newField;
        }

        public void alphabetFunction(string alphabet)
        {
            TextField.text=TextField.text + alphabet;
        }

        public void BackSpace()
        {
            if(TextField.text.Length>0) {
                TextField.text= TextField.text.Remove(TextField.text.Length-1);
            }
        }

        public void CloseAllLayouts()
        {

            EngLayoutSml.SetActive(false);
            EngLayoutBig.SetActive(false);
            SymbLayout.SetActive(false);

        }

        public void ShowLayout(GameObject SetLayout)
        {
            CloseAllLayouts();
            SetLayout.SetActive(true);
        }
    }
}