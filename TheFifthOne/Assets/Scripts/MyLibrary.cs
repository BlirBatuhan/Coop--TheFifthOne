using System.Collections.Generic;
using UnityEngine;



namespace Kutuphanem { 
public class MyLibrary 
{
        public void Sol_Hareket(Animator anim, string AnaParatme,
               List<float> ParametreDegerleri)
        {
            if (Input.GetKey(KeyCode.A))
            {
               
                if (Input.GetKey(KeyCode.W)) // Sadece W+A ise
                {
                    anim.SetFloat(AnaParatme, ParametreDegerleri[1]);
                }
                else if (Input.GetKey(KeyCode.S)) // Geri giderken sola bas?l?ysa
                {
                    anim.SetFloat(AnaParatme, ParametreDegerleri[2]);
                }
                else // Sadece sola hareket
                {
                    anim.SetFloat(AnaParatme, ParametreDegerleri[0]);
                }
            }

            if (Input.GetKeyUp(KeyCode.A))
            {
                anim.SetFloat(AnaParatme, 0);
            }
        }

        public void Sag_Hareket(Animator anim, string AnaParatme,
             List<float> ParametreDegerleri)
        {
            if (Input.GetKey(KeyCode.D))
            {
                
                if (Input.GetKey(KeyCode.W)) // Sadece W+D ise
                {
                    anim.SetFloat(AnaParatme, ParametreDegerleri[1]);
                }
                else if (Input.GetKey(KeyCode.S)) // Geri giderken sola bas?l?ysa
                {
                    anim.SetFloat(AnaParatme, ParametreDegerleri[2]);
                }
                else // Sadece sola hareket
                {
                    anim.SetFloat(AnaParatme, ParametreDegerleri[0]);
                }
            }

            if (Input.GetKeyUp(KeyCode.D))
            {
                anim.SetFloat(AnaParatme, 0);
            }
        }

        public void Geri_Hareket(Animator anim, string AnaParatme)
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                anim.SetFloat("speed", 0);
                anim.SetBool(AnaParatme, true);
            }

            if (Input.GetKeyUp(KeyCode.S))
            {
                anim.SetBool(AnaParatme, false);
            }
        }

        public void Egilme_Hareket(Animator anim, string AnaParatme,
             List<float> ParametreDegerleri)
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                
                if (Input.GetKey(KeyCode.W))
                {
                    anim.SetFloat(AnaParatme, Mathf.Lerp(anim.GetFloat(AnaParatme), ParametreDegerleri[1], Time.deltaTime * 10f));
                }
                else if (Input.GetKey(KeyCode.S))
                {
                    anim.SetFloat(AnaParatme, Mathf.Lerp(anim.GetFloat(AnaParatme), ParametreDegerleri[2], Time.deltaTime * 10f));
                }
                else if (Input.GetKey(KeyCode.A))
                {
                    anim.SetFloat(AnaParatme, Mathf.Lerp(anim.GetFloat(AnaParatme), ParametreDegerleri[3], Time.deltaTime * 10f));
                }
                else if (Input.GetKey(KeyCode.D))
                {
                    anim.SetFloat(AnaParatme, Mathf.Lerp(anim.GetFloat(AnaParatme), ParametreDegerleri[4], Time.deltaTime * 10f));
                }
                else
                {
                    anim.SetFloat(AnaParatme, ParametreDegerleri[0]);
                }
            }

            if (Input.GetKeyUp(KeyCode.LeftControl))
            {
                anim.SetFloat(AnaParatme, 0);
            }

        }

        public List<float> ParamtereOlustur(float[] parametre)
        {
            List<float> Yon_Parametreleri = new List<float>();
            foreach (float item in parametre)
            {
                Yon_Parametreleri.Add(item);
            }
            return Yon_Parametreleri;
        }
    
}
}
