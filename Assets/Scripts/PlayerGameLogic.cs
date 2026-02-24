using System.Collections.Generic;
using UnityEngine;

public enum PLayerColor
{
    gold,
    silver
}
public class PlayerGameLogic : MonoBehaviour
{
    bool isGold_ = false;
    [SerializeField] List<Material> bodyMaterialGold;
    [SerializeField] List<Material> bodyMaterialSilver;
    [SerializeField] List<Material> cloakMaterialGold;
    [SerializeField] List<Material> cloakMaterialSilver;
    [SerializeField] List<Material> hairMaterialGold;
    [SerializeField] List<Material> hairMaterialSilver;

    [SerializeField] SkinnedMeshRenderer body;
    [SerializeField] SkinnedMeshRenderer cloak;
    [SerializeField] MeshRenderer hair;

    private void Start()
    {
        changeColor();
    }
    public void changeMaterial(PLayerColor? plColor = null)
    {
        bool pureBool = isGold_;
        if (plColor == null)
        {
            isGold_ = !isGold_;
        }
        else
        {
            switch (plColor)
            {
                case PLayerColor.gold: isGold_ = true; break;
                case PLayerColor.silver: isGold_ = false; break;
            }
        }
        if (pureBool != isGold_)
        {
            changeColor();
        }
    }
    void changeColor()
    {
        if (isGold_)
        {
            body.SetMaterials(bodyMaterialGold);
            cloak.SetMaterials(cloakMaterialGold);
            hair.SetMaterials(hairMaterialGold);
        }
        else
        {
            body.SetMaterials(bodyMaterialSilver);
            cloak.SetMaterials(cloakMaterialSilver);
            hair.SetMaterials(hairMaterialSilver);
        }
    }
    public bool isGold()
    {
        return isGold_;
    }
}
