using System.Collections;

public interface IState
{
    void Enter();
    void Exit();
    void Reset();
    void Tick(float deltaTime);
    StateOutput GetStateOutput();
}
