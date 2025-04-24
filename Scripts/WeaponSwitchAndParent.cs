using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSwitchAndParent : MonoBehaviour
{
    public GameObject parent;
    public GameObject weaponHip;
    public GameObject weaponHand;
    public GameObject weapon;
    public Vector3 handPos;
    // Start is called before the first frame update
    public  void HoldSheathWeapon()
    {
        weapon.transform.parent=weaponHand.transform.parent;
        weapon.transform.position = weaponHand.transform.position;
        weapon.transform.rotation = weaponHand.transform.rotation;
    }
    public void StowSheathWeapon()
    {
        weapon.transform.parent = weaponHip.transform.parent;
        weapon.transform.position = weaponHip.transform.position;
        weapon.transform.rotation = weaponHip.transform.rotation;
    }
}
