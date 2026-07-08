using UnityEngine;
using System;
using System.Collections;


public class AudioEventListener : MonoBehaviour
{
    private IEnumerator Wait(float time, Action set)
    {
        yield return new WaitForSeconds(time);
        set?.Invoke();
    }
    private void OnEnable()
    {
        Game.Movement.PlayerController.PlayerWalk += PlayerWalkSFX;
    }
    private void OnDisable()
    {
        Game.Movement.PlayerController.PlayerWalk -= PlayerWalkSFX;
    }
    private bool _playerWalkCallable = true;
    private void PlayerWalkSFX()
    {
        //Debug.Log("Something does work!");
        if(!_playerWalkCallable) return;
        SoundManager.PlaySound(SoundType.WALK);
        _playerWalkCallable = false;
        StartCoroutine(Wait(0.25f, () => _playerWalkCallable = true));
    }
    private void PlayerPickUpSFX(Game.Items.Item? _)
    {
        SoundManager.PlaySound(SoundType.PICKUP);
    }
    private void PlayerThrowSFX()
    {
        SoundManager.PlaySound(SoundType.THROW);
    }
    private void MoneySFX()
    {
        SoundManager.PlaySound(SoundType.MONEY);
    }
    private void UpgradeSFX()
    {
        SoundManager.PlaySound(SoundType.UPGRADE);
    }
    private void ButtonSFX()
    {
        SoundManager.PlaySound(SoundType.BUTTON);
    }
    private void WaiterFlySFX()
    {
        SoundManager.PlaySound(SoundType.WAITERFLY);
    }

    private void WaiterSplatSFX()
    {
        SoundManager.PlaySound(SoundType.WAITERSPLAT);
    }

}
