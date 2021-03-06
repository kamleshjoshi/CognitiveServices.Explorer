using CognitiveServices.Explorer.Application.FaceApi;
using CognitiveServices.Explorer.Domain.Face;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CognitiveServices.Explorer.Application.ViewModels.FaceApi
{
    public class DetectViewModel : BaseFaceApiViewModel
    {
        private readonly HttpRequest _personGroupListRequest;
        private HttpRequest _detectBinaryRequest;
        private HttpRequest _detectUrlRequest;
        private HttpRequest _identifyRequest;

        public DetectViewModel(IMediator mediator)
            : base(mediator)
        {
            _personGroupListRequest = PersonGroupRequestGenerator.List();
            _detectBinaryRequest = FaceRequestGenerator.Detect(Array.Empty<byte>());
            _detectUrlRequest = FaceRequestGenerator.Detect(string.Empty);
            _identifyRequest = FaceRequestGenerator.Identify(string.Empty, Array.Empty<string>());

            UpdateRequestList();
        }

        public string? SelectedPersonGroupId { get; set; }
        public string SelectedDetectionModel { get; set; } = FaceApiConstants.DetectModelV02;
        public string SelectedRecognitionModel { get; set; } = FaceApiConstants.RecognitionModelV01;
        public List<PersonGroupDto>? PersonGroups { get; private set; }
        public List<DetectedFaceDto>? Faces { get; private set; }
        public List<IdentityCandidate>? Candidates { get; private set; }
        public List<HttpRequest> Requests { get; } = new List<HttpRequest>();

        public async override Task OnInitializedAsync()
        {
            await base.OnInitializedAsync().ConfigureAwait(false);

            PersonGroups = await MakeRequest<List<PersonGroupDto>>(_personGroupListRequest);
            SelectedPersonGroupId = PersonGroups?.Select(g => g.PersonGroupId).FirstOrDefault();
        }

        public async Task ProfileChanged()
        {
            // This information is no longer valid as the Face IDs are not shared between accounts.
            Faces = null;
            Candidates = null;

            await LoadLatestConfig();
            PersonGroups = await MakeRequest<List<PersonGroupDto>>(_personGroupListRequest);
        }

        public async Task Detect(byte[] data)
        {
            Faces = null;
            Candidates = null;

            _detectBinaryRequest = FaceRequestGenerator.Detect(
                data,
                detectionModel: SelectedDetectionModel,
                recognitionModel: SelectedRecognitionModel);

            UpdateRequestList();

            Faces = await MakeRequest<List<DetectedFaceDto>>(_detectBinaryRequest).ConfigureAwait(false)
                ?? new List<DetectedFaceDto>(0);
        }

        public async Task Detect(string imageUrl)
        {
            Faces = null;
            Candidates = null;

            _detectUrlRequest = FaceRequestGenerator.Detect(
                imageUrl,
                detectionModel: SelectedDetectionModel,
                recognitionModel: SelectedRecognitionModel);

            UpdateRequestList();

            Faces = await MakeRequest<List<DetectedFaceDto>>(_detectUrlRequest).ConfigureAwait(false)
                ?? new List<DetectedFaceDto>(0);
        }

        public async Task Identify()
        {
            if (string.IsNullOrWhiteSpace(SelectedPersonGroupId))
            {
                Error = "Person Group Id is not set!";
                return;
            }

            Candidates = null;

            _identifyRequest = FaceRequestGenerator.Identify(SelectedPersonGroupId!, Faces.Select(f => f.faceId));
            UpdateRequestList();

            Candidates = await MakeRequest<List<IdentityCandidate>>(_identifyRequest).ConfigureAwait(false)
                ?? new List<IdentityCandidate>(0);
        }

        private void UpdateRequestList()
        {
            // Both requests are changing every time we do a request and because the reference is changing, we need to rebuild the list.
            Requests.Clear();
            Requests.Add(_detectBinaryRequest);
            Requests.Add(_detectUrlRequest);
            Requests.Add(_identifyRequest);
            Requests.Add(_personGroupListRequest);
        }
    }
}
