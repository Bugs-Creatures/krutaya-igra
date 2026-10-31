using UnityEngine;

public class PlayerStepSound : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _audioClip;
    void Start()
    {
        
    }
    void Update()
    {
        
    }
    public void PlaySound()
    {
        _audioSource.PlayOneShot(_audioClip);
    }
}