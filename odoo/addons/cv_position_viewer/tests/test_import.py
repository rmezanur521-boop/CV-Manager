from unittest.mock import MagicMock, patch
import requests
from odoo.exceptions import UserError
from odoo.tests.common import TransactionCase


class TestPositionImport(TransactionCase):

    def setUp(self):
        super().setUp()
        self.wizard_model = self.env['cv.position.import.wizard']
        self.position_model = self.env['cv.position']

    @patch('requests.get')
    def test_successful_import(self, mock_get):
        mock_response = MagicMock()
        mock_response.status_code = 200
        mock_response.json.return_value = {
            'id': 101,
            'title': 'Senior .NET Developer',
            'company': 'Tech Corp',
            'level': 'Senior',
            'shortDescription': 'Great opportunity for .NET devs.',
            'publishedCvCount': 5,
            'generatedAt': '2026-10-04T12:00:00Z',
            'attributes': [
                {
                    'attributeId': 10,
                    'name': 'Years Experience',
                    'type': 'Numeric',
                    'isRequired': True,
                    'responseCount': 5,
                    'summary': 'Min: 2.00, Max: 8.00, Avg: 5.20',
                    'numeric': {
                        'min': 2.0,
                        'max': 8.0,
                        'average': 5.2,
                    },
                },
                {
                    'attributeId': 11,
                    'name': 'Primary Framework',
                    'type': 'Dropdown',
                    'isRequired': False,
                    'responseCount': 5,
                    'summary': '.NET: 3, React: 2',
                    'dropdown': {
                        'topOptions': [
                            {'value': '.NET', 'count': 3},
                            {'value': 'React', 'count': 2},
                        ],
                    },
                },
            ],
        }
        mock_get.return_value = mock_response

        wizard = self.wizard_model.create({
            'api_base_url': 'http://localhost:5000',
            'api_token': 'cvp_live_testtoken123',
        })
        action = wizard.action_import()

        self.assertEqual(action.get('res_model'), 'cv.position')
        position = self.position_model.browse(action.get('res_id'))
        self.assertTrue(position.exists())
        self.assertEqual(position.external_id, 101)
        self.assertEqual(position.title, 'Senior .NET Developer')
        self.assertEqual(position.company, 'Tech Corp')
        self.assertEqual(position.level, 'Senior')
        self.assertEqual(position.published_cv_count, 5)
        self.assertEqual(len(position.attribute_ids), 2)

        num_attr = position.attribute_ids.filtered(lambda a: a.attribute_id == 10)
        self.assertEqual(num_attr.numeric_min, 2.0)
        self.assertEqual(num_attr.numeric_max, 8.0)
        self.assertEqual(num_attr.numeric_average, 5.2)

        dropdown_attr = position.attribute_ids.filtered(lambda a: a.attribute_id == 11)
        self.assertEqual(len(dropdown_attr.value_breakdown_ids), 2)
        top_option = dropdown_attr.value_breakdown_ids.filtered(lambda v: v.value == '.NET')
        self.assertEqual(top_option.count, 3)

    @patch('requests.get')
    def test_unauthorized_token(self, mock_get):
        mock_response = MagicMock()
        mock_response.status_code = 401
        mock_get.return_value = mock_response

        wizard = self.wizard_model.create({
            'api_base_url': 'http://localhost:5000',
            'api_token': 'cvp_live_badtoken',
        })
        with self.assertRaises(UserError):
            wizard.action_import()

    @patch('requests.get')
    def test_position_not_found(self, mock_get):
        mock_response = MagicMock()
        mock_response.status_code = 404
        mock_get.return_value = mock_response

        wizard = self.wizard_model.create({
            'api_base_url': 'http://localhost:5000',
            'api_token': 'cvp_live_nonexistent',
        })
        with self.assertRaises(UserError):
            wizard.action_import()

    @patch('requests.get')
    def test_rate_limit_exceeded(self, mock_get):
        mock_response = MagicMock()
        mock_response.status_code = 429
        mock_get.return_value = mock_response

        wizard = self.wizard_model.create({
            'api_base_url': 'http://localhost:5000',
            'api_token': 'cvp_live_ratelimited',
        })
        with self.assertRaises(UserError):
            wizard.action_import()

    @patch('requests.get')
    def test_connection_error(self, mock_get):
        mock_get.side_effect = requests.exceptions.ConnectionError('Failed to connect')

        wizard = self.wizard_model.create({
            'api_base_url': 'http://invalid.host:5000',
            'api_token': 'cvp_live_anytoken',
        })
        with self.assertRaises(UserError):
            wizard.action_import()
