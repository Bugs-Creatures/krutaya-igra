using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class CopyTutorialStyle
{
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode first");
        var hud=Object.FindAnyObjectByType<PlayerTutorialHud>();var source=hud.suspiciousCard;
        Undo.RecordObject(hud,"Copy current Suspicious style");
        TutorialCardView Copy(TutorialCardView old,string name,string title,string body)
        {
            int index=old.transform.GetSiblingIndex();var instance=Object.Instantiate(source.gameObject,source.transform.parent,false);instance.name=name;Undo.RegisterCreatedObjectUndo(instance,"Copy tutorial card");
            var card=instance.GetComponent<TutorialCardView>();card.title.text=title;card.body.text=body;card.gameObject.SetActive(false);card.transform.SetSiblingIndex(index);
            if(!card.counter.transform.IsChildOf(card.transform)||!card.actionButton.IsChildOf(card.transform))throw new System.Exception("Card references not copied correctly");
            Undo.DestroyObjectImmediate(old.gameObject);return card;
        }
        hud.mapCard=Copy(hud.mapCard,"Map","КАРТА","Чтобы открыть карту нажмите \"M\".");
        hud.droppedCard=Copy(hud.droppedCard,"DroppedItems","ПОИСК ПРЕДМЕТОВ","Чтобы подсветить предметы, которые вы уронили, удерживайте \"ПКМ\".");
        EditorUtility.SetDirty(hud);EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);EditorSceneManager.SaveScene(hud.gameObject.scene);
    }
}
