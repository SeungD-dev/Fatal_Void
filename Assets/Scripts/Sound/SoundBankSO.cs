using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundBankSO", menuName = "Scriptable Objects/SoundBankSO")]
public class SoundBankSO : ScriptableObject
{
    public List<Sound> sounds = new List<Sound>();
}
