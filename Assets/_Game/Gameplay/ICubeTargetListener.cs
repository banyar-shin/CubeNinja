namespace CubeNinja.Gameplay
{
    public interface ICubeTargetListener
    {
        void OnCubeClicked(CubeTarget target);
        void OnCubeMissed(CubeTarget target);
    }
}
