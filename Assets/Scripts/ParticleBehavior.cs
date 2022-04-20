using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleBehavior : MonoBehaviour
{
    void Move()
    {
        //float speed = Random.value * 5;
        //Vector3 vel = new Vector3(Random.value - 0.5f, Random.value - 0.5f, 0) * Time.deltaTime * speed;
        //if (transform.position.x + vel.x > CPUParticleManager.ParticleManager.width || transform.position.x + vel.x < 0) // handle bundary
        //    vel.x = -vel.x;
        //if (transform.position.y + vel.y > CPUParticleManager.ParticleManager.height || transform.position.y + vel.y < 0)
        //    vel.y = -vel.y;
        //transform.position += vel;
    }

    private void Update()
    {
        Move();
    }
}
