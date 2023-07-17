using ObjCRuntime;
using Metal;
using CoreAnimation;
using System.Numerics;
using CoreVideo;

namespace TestMac;

public partial class ViewController : NSViewController {

	CAMetalLayer? Layer;
	IMTLBuffer? VBO;
	IMTLRenderPipelineState? PipelineRenderState;
	IMTLCommandQueue? CommandQueue;
	protected ViewController (NativeHandle handle) : base (handle)
	{
		// This constructor is required if the view controller is loaded from a xib or a storyboard.
		// Do not put any initialization here, use ViewDidLoad instead.
	}

	public override void ViewDidLoad ()
	{
		base.ViewDidLoad ();
		Layer = new CAMetalLayer();
		Layer.Device = MTLDevice.SystemDefault;
		Layer.PixelFormat = MTLPixelFormat.BGRA8Unorm;
		Layer.FramebufferOnly = true;
		Layer.Frame = View.Layer.Frame;

		View.Layer.AddSublayer(Layer);
		Vertex[] vertices = new Vertex[]
		{
			new Vertex {Location = new (0, 1, 0, 1), Color = new (1, 0, 0, 1)},
			new Vertex {Location = new (-1, -1, 0, 1), Color = new (0, 1, 0, 1)},
			new Vertex {Location = new (1, -1, 0, 1), Color = new (0, 0, 1, 1)},
		};
        VBO = Layer.Device.CreateBuffer(vertices, new MTLResourceOptions());
		var lib = Layer.Device.CreateDefaultLibrary();
		var fs = lib.CreateFunction("basic_fragment");
		var vs = lib.CreateFunction("basic_vertex");

		var PipelineStateDescriptor = new MTLRenderPipelineDescriptor();
        PipelineStateDescriptor.VertexFunction = vs;
        PipelineStateDescriptor.FragmentFunction = fs;
        PipelineStateDescriptor.ColorAttachments[0].PixelFormat = MTLPixelFormat.BGRA8Unorm;

        PipelineRenderState = Layer.Device.CreateRenderPipelineState(PipelineStateDescriptor, out var error);

        CommandQueue = Layer.Device.CreateCommandQueue();

		var timer = new CVDisplayLink();

		_ = Fun();

    }
	

    public void Render()
	{
		var drawable = Layer.NextDrawable();
		if (drawable == null)
			return;
		var passDescriptor = new MTLRenderPassDescriptor();
		passDescriptor.ColorAttachments[0].Texture = drawable.Texture;
        passDescriptor.ColorAttachments[0].LoadAction =  MTLLoadAction.Clear;
        passDescriptor.ColorAttachments[0].ClearColor = new MTLClearColor(0, 104.0f / 255.0f, 55.0 / 255.0, 1);
		var bufferDescriptor = new MTLCommandBufferDescriptor();
        var commandBuffer = CommandQueue.CreateCommandBuffer(bufferDescriptor);
		var renderEncoder = commandBuffer.CreateRenderCommandEncoder(passDescriptor);
		renderEncoder.SetRenderPipelineState(PipelineRenderState);
		renderEncoder.SetVertexBuffer(VBO, 0, 0);
		renderEncoder.DrawPrimitives(MTLPrimitiveType.Triangle, 0, 3, 1);
		renderEncoder.EndEncoding();

		commandBuffer.PresentDrawable(drawable);
		commandBuffer.Commit();


    }

	bool IsExit;
	public async Task Fun()
	{
		await Task.Delay(100);
		while(IsExit == false)
		{
			await Task.Yield();
			Render();

        }
	}
	public override NSObject RepresentedObject {
		get => base.RepresentedObject;
		set {
			base.RepresentedObject = value;

			// Update the view, if already loaded.
		}
	}
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
		IsExit = true;
    }
}

public struct Vertex
{
	public Vector4 Location;
	public Vector4 Color;
}