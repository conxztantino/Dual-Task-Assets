using UnityEngine;

public class Collider : MonoBehaviour
{
   public PointCollider pointCollider;

    void OnTriggerEnter(Collider colisor){

        pointCollider.Score++;
        pointCollider.ScoreText.text = pointCollider.Score.ToString();
    }
    

}
